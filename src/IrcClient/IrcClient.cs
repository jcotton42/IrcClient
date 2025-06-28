using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipelines;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using IrcClient.Messages;

namespace IrcClient;

public sealed class IrcClient
{
    private readonly Stream stream;

    private readonly Channel<RawIrcMessage> inboundMessages = Channel.CreateUnbounded<RawIrcMessage>(
        new UnboundedChannelOptions
        {
            SingleWriter = true,
        });

    private readonly Channel<RawIrcMessage> outboundMessages = Channel.CreateUnbounded<RawIrcMessage>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
        });

    private IrcClient(Stream stream) => this.stream = stream;

    public static async Task<IrcClient> CreateAsync(string host, int port, bool useSsl,
        CancellationToken cancellationToken)
    {
        Socket? socket = null;
        Stream? stream = null;
        try
        {
            socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(host, port, cancellationToken);

            stream = new NetworkStream(socket, ownsSocket: true);
            socket = null;
            if (useSsl)
            {
                var sslStream = new SslStream(stream, leaveInnerStreamOpen: false);
                await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                {
                    TargetHost = host,
                }, cancellationToken);
                stream = sslStream;
            }
        }
        catch
        {
            stream?.Dispose();
            socket?.Dispose();
            throw;
        }

        return new IrcClient(stream);
    }

    public async Task ClientLoop(CancellationToken cancellationToken)
    {
        var receive = Task.Run(() => ReceiveLoop(cancellationToken), cancellationToken);
        var send = Task.Run(() => SendLoop(cancellationToken), cancellationToken);

        await Task.WhenAll(receive, send);
    }

    private async Task ReceiveLoop(CancellationToken cancellationToken)
    {
        var pipe = PipeReader.Create(stream);
        while (!cancellationToken.IsCancellationRequested)
        {
            var result = await pipe.ReadAsync(cancellationToken);
            var buffer = result.Buffer;
            while (TryReadMessage(ref buffer, out var message))
            {
                _ = inboundMessages.Writer.TryWrite(message);
            }

            pipe.AdvanceTo(buffer.Start, buffer.End);

            if (result.IsCompleted)
            {
                break;
            }
        }

        await pipe.CompleteAsync();
        inboundMessages.Writer.Complete();
        return;

        bool TryReadMessage(ref ReadOnlySequence<byte> buffer, [NotNullWhen(true)] out RawIrcMessage? message)
        {
            var reader = new SequenceReader<byte>(buffer);
            if (reader.TryReadTo(out ReadOnlySequence<byte> line, "\r\n"u8, advancePastDelimiter: true))
            {
                buffer = buffer.Slice(reader.Position);
                message = RawIrcMessage.Parse(Encoding.UTF8.GetString(line));
                return true;
            }

            message = null;
            return false;
        }
    }

    private async Task SendLoop(CancellationToken cancellationToken)
    {
        while (await outboundMessages.Reader.WaitToReadAsync(cancellationToken))
        {
            while (outboundMessages.Reader.TryRead(out var message))
            {
                var bytes = Encoding.UTF8.GetBytes(message + "\r\n");
                await stream.WriteAsync(bytes, cancellationToken);
            }
        }
    }
}
