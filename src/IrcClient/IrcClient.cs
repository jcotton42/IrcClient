using System.Threading.Channels;
using System.Threading.RateLimiting;

using IrcClient.Features.Registration;
using IrcClient.Infrastructure;

namespace IrcClient;

public sealed class IrcClient : IAsyncDisposable
{
    private readonly IrcConnection _connection;
    private readonly SemaphoreSlim _counter;
    private readonly ResponderDispatch _dispatch;
    private readonly IReadOnlyDictionary<string, ITypedMessageFactory> _messageFactories;
    private readonly RateLimiter _rateLimiter;
    private readonly Channel<IrcMessage> _toSend;
    private readonly Channel<IrcMessage> _toSendImmediately;

    public IrcClientState State { get; } = new();
    public IrcClientOptions Options { get; }

    public IrcClient(IrcClientOptions options, ResponderDispatch dispatch, IEnumerable<ITypedMessageFactory> typedMessageFactories)
    {
        _connection = new IrcConnection();
        _counter = new SemaphoreSlim(0);
        _dispatch = dispatch;
        Options = options;
        // TODO maybe move this to the options?
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

        var factories = new Dictionary<string, ITypedMessageFactory>();
        foreach (var factory in typedMessageFactories)
        {
            if (!factories.TryAdd(factory.Command.ToUpperInvariant(), factory))
            {
                throw new ArgumentException($"Duplicate message factory for {factory.Command}",
                    nameof(typedMessageFactories));
            }
        }

        _messageFactories = factories;
    }

    public async Task RunAsync(CancellationToken stopToken)
    {
        await _connection.ConnectAsync(Options.Hostname, Options.Port, Options.UseSsl, stopToken);
        // TODO use separate tokens for these so I can send QUIT when stopToken is tripped before disconnecting
        var sendTask = SendLoop(stopToken);
        var receiveTask = ReceiveLoop(stopToken);

        State.Status = ClientStatus.Registering;
        SendMessageImmediately(IrcMessage.Factory.CapLs());
        if (Options.Password is not null)
        {
            SendMessageImmediately(IrcMessage.Factory.Pass(Options.Password));
        }

        SendMessageImmediately(IrcMessage.Factory.Nick(Options.Nick));
        SendMessageImmediately(IrcMessage.Factory.User(Options.Username, Options.Realname));

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
        while (!stopToken.IsCancellationRequested)
        {
            var message = await _connection.ReceiveMessageAsync(stopToken);
            // TODO log if this returns false (unknown message type)?
            var typedMessage = _messageFactories.TryGetValue(message.Command.ToUpperInvariant(), out var factory)
                ? factory.FromMessage(message)
                : message;
            // TODO maybe move the CAP/AUTHENTICATE/other registration stuff here, it's so heavily intertwined with
            // the client anyhow
            _dispatch.Dispatch(message);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _counter.Dispose();
        await _connection.DisposeAsync();
    }

    internal void EnableCapabilities(IReadOnlySet<string> enabledCapabilities)
    {
        if (enabledCapabilities.Contains("sasl"))
        {
            State.IsSaslAvailable = true;
            // pre-3.2 servers don't tell us ahead of time what SASL mechanisms they support, in which case the list
            // will be null. PLAIN is a good default
            if (ShouldAuthenticate())
            {
                SendMessageImmediately(IrcMessage.Factory.Authenticate("PLAIN"));
                State.Status = ClientStatus.Authenticating;
            }
        }
    }

    internal void DisableCapabilities(IReadOnlySet<string> removedCapabilities)
    {
        if (removedCapabilities.Contains("sasl")) State.IsSaslAvailable = false;
    }

    internal void ProcessCapabilityMetadata(IReadOnlyDictionary<string, string?> capabilities)
    {
        if (capabilities.GetValueOrDefault("sasl") is { } saslMechs)
        {
            State.SupportedSaslMechanisms = saslMechs.Split(',', StringSplitOptions.RemoveEmptyEntries);
        }
    }

    internal bool ShouldAuthenticate() =>
        Options.SaslPlain is not null
        && (State.SupportedSaslMechanisms is null || State.SupportedSaslMechanisms.Contains("PLAIN"));
}
