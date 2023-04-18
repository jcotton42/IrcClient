using IrcClient.Infrastructure;

namespace IrcClient.Features.Ping;

public sealed class Ping : TypedMessage
{
    public required string Token { get; init; }

    public Ping(IrcMessage message) : base(message) { }

    public sealed class Factory : ITypedMessageFactory
    {
        public string Command => "PING";
        public IMessage FromMessage(IrcMessage message) => new Ping(message) { Token = message.Parameters[0] };
    }
}

public sealed class Pong : TypedMessage
{
    public required string Server { get; init; }
    public required string Token { get; init; }

    public Pong(IrcMessage message) : base(message) { }

    public sealed class Factory : ITypedMessageFactory
    {
        public string Command => "PONG";

        public IMessage FromMessage(IrcMessage message) =>
            new Pong(message) { Server = message.Parameters[0], Token = message.Parameters[1] };
    }
}

public static class PingMessageFactory
{
    public static IrcMessage Ping(this IrcMessageFactory _, string token) => IrcMessage.Create("PING", token);
    public static IrcMessage Pong(this IrcMessageFactory _, string token) => IrcMessage.Create("PONG", token);
}
