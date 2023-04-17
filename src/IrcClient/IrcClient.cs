using System.Threading.Channels;
using System.Threading.RateLimiting;

using IrcClient.Features.Registration;

namespace IrcClient;

public sealed class IrcClient : IAsyncDisposable
{
    private readonly IrcConnection _connection;
    private readonly SemaphoreSlim _counter;
    private readonly IrcClientOptions _options;
    private readonly RateLimiter _rateLimiter;
    private readonly Channel<IrcMessage> _toSend;
    private readonly Channel<IrcMessage> _toSendImmediately;

    public IrcClientState State { get; } = new();

    public IrcClient(IrcClientOptions options)
    {
        _connection = new IrcConnection();
        _counter = new SemaphoreSlim(0);
        _options = options;
        _rateLimiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            AutoReplenishment = true,
            PermitLimit = 3,
            QueueLimit = int.MaxValue,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            Window = TimeSpan.FromSeconds(3),
        });
        _toSend = Channel.CreateUnbounded<IrcMessage>(new UnboundedChannelOptions { SingleReader = true });
        _toSendImmediately = Channel.CreateUnbounded<IrcMessage>(new UnboundedChannelOptions { SingleReader = true });
    }

    public async Task RunAsync(CancellationToken stopToken)
    {
        await _connection.ConnectAsync(_options.Hostname, _options.Port, _options.UseSsl, stopToken);
        var sendTask = SendLoop(stopToken);
        var receiveTask = ReceiveLoop(stopToken);

        State.Status = ClientStatus.Registering;
        SendMessageImmediately(IrcMessage.Factory.CapLs());
        if (_options.Password is not null)
        {
            SendMessageImmediately(IrcMessage.Factory.Pass(_options.Password));
        }

        SendMessageImmediately(IrcMessage.Factory.Nick(_options.Nick));
        SendMessageImmediately(IrcMessage.Factory.User(_options.Username, _options.Realname));

        try
        {
            await Task.WhenAll(sendTask, receiveTask);
        }
        catch (OperationCanceledException oce) when (oce.CancellationToken == stopToken) { }
    }

    public void SendMessage(IrcMessage message)
    {
        _toSend.Writer.TryWrite(message);
        _counter.Release();
    }

    public void SendMessageImmediately(IrcMessage message)
    {
        _toSendImmediately.Writer.TryWrite(message);
        _counter.Release();
    }

    private async Task SendLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await _counter.WaitAsync(token);
            if (_toSendImmediately.Reader.TryRead(out var message))
            {
                await _connection.SendMessageAsync(message, token);
            }
            else if (_toSend.Reader.TryRead(out message))
            {
                await _rateLimiter.AcquireAsync(permitCount: 1, token);
                await _connection.SendMessageAsync(message, token);
            }
            else
            {
                throw new InvalidOperationException("Release on the counter called too many times.");
            }
        }
    }

    private async Task ReceiveLoop(CancellationToken stopToken)
    {
        var message = await _connection.ReceiveMessageAsync(stopToken);
        // TODO dispatch message
    }

    public async ValueTask DisposeAsync()
    {
        _counter.Dispose();
        await _connection.DisposeAsync();
    }

    internal void EnableCapabilities(IReadOnlySet<string> enabledCapabilities)
    {
        if (enabledCapabilities.Contains("sasl")) State.SaslAvailable = true;
    }

    internal void DisableCapabilities(IReadOnlySet<string> removedCapabilities)
    {
        if (removedCapabilities.Contains("sasl")) State.SaslAvailable = false;
    }

    internal void ProcessCapabilityMetadata(IReadOnlyDictionary<string, string?> capabilities)
    {
        if (capabilities.GetValueOrDefault("sasl") is { } saslMechs)
        {
            State.SupportedSaslMechanisms = saslMechs.Split(',', StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
