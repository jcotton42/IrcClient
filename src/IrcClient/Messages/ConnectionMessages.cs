using System;
using System.Collections.Immutable;
using System.ComponentModel;

namespace IrcClient.Messages;

// TODO CAP and friends

[IrcMessage("AUTHENTICATE")]
public sealed partial record AuthenticateMessage;

public sealed partial record AuthenticateMessage
{
    public static AuthenticateMessage FromRaw(RawIrcMessage raw) => new();
}

[IrcMessage("PASS")]
public sealed partial record PassMessage(string Password);

[IrcMessage("NICK")]
public sealed partial record NickMessage(string Nickname);

[IrcMessage("USER")]
public sealed partial record UserMessage(string Username, string RealName)
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public UserMessage(string username, string dummy1, string dummy2, string realName) : this(username, realName) {}
}

public partial record UserMessage : IIrcMessage
{
    public required ImmutableDictionary<string, string?> Tags { get; init; }
    public required IrcMessageSource? Source { get; init; }
    public string Command => "USER";

    public static UserMessage FromRaw(RawIrcMessage raw)
    {
        switch (raw.Parameters.Length)
        {
            case 2:
                return new UserMessage(raw.Parameters[0], raw.Parameters[1], raw.Parameters[2], raw.Parameters[3])
                {
                    Tags = raw.Tags,
                    Source = raw.Source,
                };
            case 4:
                return new UserMessage(raw.Parameters[0], raw.Parameters[1])
                {
                    Tags = raw.Tags,
                    Source = raw.Source,
                };
        }

        throw new FormatException("USER message ill-formed");
    }
}

[IrcMessage("PING")]
public sealed partial record PingMessage(string Token);

[IrcMessage("PONG")]
public sealed partial record PongMessage(string? Server, string Token)
{
    public PongMessage(string token) : this(null, token) { }
}

[IrcMessage("OPER")]
public sealed partial record OperMessage(string Name, string Password);

[IrcMessage("QUIT")]
public sealed partial record QuitMessage(string? Reason = null);

[IrcMessage("ERROR")]
public sealed partial record ErrorMessage(string Reason);
