using IrcClient.Infrastructure;

namespace IrcClient.Features.Ping;

public sealed class Ping : IFromMessage<Ping>
{
    public required string Token { get; init; }

    public static string Command => "PING";

    public static Ping FromMessage(IrcMessage message) => new() { Token = message.Parameters[0] };
}

public sealed class Pong : IFromMessage<Pong>
{
    public required string Server { get; init; }
    public required string Token { get; init; }

    public static string Command => "PONG";

    public static Pong FromMessage(IrcMessage message) =>
        new Pong { Server = message.Parameters[0], Token = message.Parameters[1] };
}

public static class PingMessageFactory
{
    public static IrcMessage Ping(this IrcMessageFactory _, string token) => IrcMessage.Create("PING", token);
    public static IrcMessage Pong(this IrcMessageFactory _, string token) => IrcMessage.Create("PONG", token);
}
