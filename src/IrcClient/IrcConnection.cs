using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

namespace IrcClient;

/// <summary>
/// An connection to an IRC server.
/// </summary>
/// <remarks>
/// <see cref="DisconnectAsync"/> does not send the QUIT message. You must do that yourself first.
/// </remarks>
public sealed class IrcConnection : IAsyncDisposable
{
    private PipeReader? _pipe;
    private Stream? _stream;

    public async Task ConnectAsync(string hostname, int port, bool useSsl, CancellationToken token)
    {
        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        await socket.ConnectAsync(hostname, port, token);
        _stream = new NetworkStream(socket, ownsSocket: true);

        if (useSsl)
        {
            var sslStream = new SslStream(_stream, leaveInnerStreamOpen: false);
            await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
            {
                TargetHost = hostname,
            }, token);
            _stream = sslStream;
        }

        _pipe = PipeReader.Create(_stream, new StreamPipeReaderOptions(leaveOpen: true));
    }

    public async Task<IrcMessage> ReceiveMessageAsync(CancellationToken token)
    {
        AssertConnected();

        while (true)
        {
            var result = await _pipe.ReadAsync(token);
            var buffer = result.Buffer;

            if (!TryReadMessage(buffer, out var consumed, out var message))
            {
                _pipe.AdvanceTo(consumed: consumed, examined: buffer.End);
                continue;
            }

            _pipe.AdvanceTo(consumed: consumed, examined: consumed);
            if (result.IsCompleted)
            {
                await _pipe.CompleteAsync();
            }
            return message;
        }
    }

    public async Task SendMessageAsync(IrcMessage message, CancellationToken token)
    {
        AssertConnected();
        var bytes = Encoding.UTF8.GetBytes(message.ToString());
        await _stream.WriteAsync(bytes, token);
    }

    public Task DisconnectAsync() => DisposeAsync().AsTask();

    [MemberNotNull(nameof(_pipe), nameof(_stream))]
    private void AssertConnected()
    {
        if (_stream is null || _pipe is null) throw new InvalidOperationException("Not connected to an IRC network");
    }

    private bool TryReadMessage(ReadOnlySequence<byte> buffer, out SequencePosition consumed, [NotNullWhen(true)] out IrcMessage? message)
    {
        message = default;
        var reader = new SequenceReader<byte>(buffer);
        if (reader.TryReadTo(out ReadOnlySequence<byte> read, delimiter: "\r\n"u8, advancePastDelimiter: true))
        {
            consumed = reader.Position;
            message = IrcMessage.Parse(Encoding.UTF8.GetString(read));
            return true;
        }

        consumed = reader.Position;
        return false;
    }

    public async ValueTask DisposeAsync()
    {
        if (_pipe is null || _stream is null) return;
        await _pipe.CompleteAsync();
        await _stream.DisposeAsync();

        _stream = null;
        _pipe = null;
    }
}
