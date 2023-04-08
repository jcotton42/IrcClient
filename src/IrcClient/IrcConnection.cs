using System.Buffers;
using System.IO.Pipelines;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;

namespace IrcClient;

/// <summary>
/// An connection to an IRC server.
/// </summary>
/// <remarks>
/// <see cref="DisconnectAsync"/> does not send the QUIT message. You must do that yourself first.
/// </remarks>
public sealed class IrcConnection : IAsyncDisposable
{
    private readonly CancellationTokenSource _disconnect;
    private readonly Channel<IrcMessage> _incoming;
    private readonly Channel<IrcMessage> _outgoing;

    private Task? _sendTask;
    private Task? _receiveTask;
    private Stream? _stream;

    public IrcConnection()
    {
        _disconnect = new CancellationTokenSource();
        _incoming = Channel.CreateUnbounded<IrcMessage>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
        _outgoing = Channel.CreateUnbounded<IrcMessage>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
    }

    public async Task ConnectAsync(string hostname, int port, bool useSsl)
    {
        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        await socket.ConnectAsync(hostname, port, _disconnect.Token);
        _stream = new NetworkStream(socket, ownsSocket: true);

        if (useSsl)
        {
            var sslStream = new SslStream(_stream, leaveInnerStreamOpen: false);
            await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
            {
                TargetHost = hostname,
            }, _disconnect.Token);
            _stream = sslStream;
        }

        _sendTask = SendLoop();
        _receiveTask = ReceiveLoop();
    }

    public ValueTask<IrcMessage> ReceiveMessageAsync(CancellationToken cancelToken = default)
    {
        using var merged = CancellationTokenSource.CreateLinkedTokenSource(_disconnect.Token, cancelToken);
        return _incoming.Reader.ReadAsync(merged.Token);
    }

    public void SendMessage(IrcMessage message)
    {
        // channel is unbounded, no need to check the return value of TryWrite
        _outgoing.Writer.TryWrite(message);
    }

    public async Task DisconnectAsync()
    {
        _disconnect.Cancel();
        if (_stream is null) return;

        try
        {
            await Task.WhenAll(_sendTask!, _receiveTask!);
        }
        catch (OperationCanceledException) {}

        await _stream.DisposeAsync();
    }

    private async Task ReceiveLoop()
    {
        if (_stream is null)
        {
            throw new InvalidOperationException("Not connected to an IRC network");
        }

        var pipe = new Pipe();
        var writer = FillPipeAsync(pipe.Writer);
        var reader = ReadPipeAsync(pipe.Reader);

        await Task.WhenAll(writer, reader);
    }

    private async Task SendLoop()
    {
        while (!_disconnect.IsCancellationRequested)
        {
            var message = await _outgoing.Reader.ReadAsync(_disconnect.Token);
            var bytes = Encoding.UTF8.GetBytes(message.ToString());
            await _stream!.WriteAsync(bytes, _disconnect.Token);
        }
    }

    private async Task FillPipeAsync(PipeWriter writer)
    {
        const int bufferSize = 1024;

        try
        {
            while (!_disconnect.IsCancellationRequested)
            {
                var memory = writer.GetMemory(bufferSize);
                var bytesRead = await _stream!.ReadAsync(memory, _disconnect.Token);
                if (bytesRead == 0) break;
                writer.Advance(bytesRead);
                var result = await writer.FlushAsync(_disconnect.Token);
                if (result.IsCompleted) break;
            }
        }
        finally
        {
            await writer.CompleteAsync();
        }
    }

    private async Task ReadPipeAsync(PipeReader reader)
    {
        try
        {
            while (!_disconnect.IsCancellationRequested)
            {
                var result = await reader.ReadAsync(_disconnect.Token);
                var buffer = result.Buffer;

                var consumed = ReadLines(buffer);
                reader.AdvanceTo(consumed: consumed, examined: buffer.End);

                if (result.IsCompleted) break;
            }
        }
        finally
        {
            await reader.CompleteAsync();
            _incoming.Writer.Complete();
        }
    }

    private SequencePosition ReadLines(ReadOnlySequence<byte> buffer)
    {
        var reader = new SequenceReader<byte>(buffer);
        while (reader.TryReadTo(out ReadOnlySequence<byte> read, delimiter: "\r\n"u8, advancePastDelimiter: true))
        {
            // the channel is unbounded, so need to check return value of TryWrite
            _incoming.Writer.TryWrite(IrcMessage.Parse(Encoding.UTF8.GetString(read)));
        }

        return reader.Position;
    }

    public async ValueTask DisposeAsync()
    {
        _disconnect.Cancel();
        _disconnect.Dispose();
        if (_stream is not null)
        {
            await DisconnectAsync();
            await _stream.DisposeAsync();
        }
    }
}
