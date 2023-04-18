using IrcClient.Infrastructure;

namespace IrcClient.Features.Registration;

public abstract class Cap : TypedMessage
{
    private Cap(IrcMessage message) : base(message) { }

    public sealed class Ls : Cap
    {
        public required IReadOnlyDictionary<string, string?> AvailableCapabilities { get; init; }
        public required bool MoreComing { get; init; }

        public Ls(IrcMessage message) : base(message) { }

        public static Ls FromMessageInner(IrcMessage message)
        {
            var moreComing = message.Parameters[2] == "*";
            var capList = moreComing ? message.Parameters[3] : message.Parameters[2];
            var caps = new Dictionary<string, string?>();
            foreach (var cap in capList.Split(' ',
                         StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                var split = cap.Split('=', 2);
                if (split.Length == 0) caps[split[0]] = null;
                else caps[split[0]] = split[1];
            }

            return new Ls(message) { AvailableCapabilities = caps, MoreComing = moreComing };
        }
    }

    public sealed class List : Cap
    {
        public required bool MoreComing { get; init; }
        public required IReadOnlySet<string> EnabledCapabilities { get; init; }

        public List(IrcMessage message) : base(message) { }

        public static List FromMessageInner(IrcMessage message)
        {
            var moreComing = message.Parameters[2] == "*";
            var caps = moreComing ? message.Parameters[3] : message.Parameters[2];
            return new List(message)
            {
                MoreComing = moreComing,
                EnabledCapabilities = new HashSet<string>(caps.Split(' ',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)),
            };
        }
    }

    public sealed class Ack : Cap
    {
        public required bool MoreComing { get; init; }
        public required IReadOnlySet<string> EnabledCapabilities { get; init; }
        public required IReadOnlySet<string> DisabledCapabilities { get; init; }

        public Ack(IrcMessage message) : base(message) { }

        public static Ack FromMessageInner(IrcMessage message)
        {
            var moreComing = message.Parameters[2] == "*";
            var caps = moreComing ? message.Parameters[3] : message.Parameters[2];
            var enabledCaps = new HashSet<string>();
            var disabledCaps = new HashSet<string>();

            foreach (var cap in caps.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                if (cap.StartsWith("-")) disabledCaps.Add(cap[1..]);
                else enabledCaps.Add(cap);
            }

            return new Ack(message)
            {
                MoreComing = moreComing, EnabledCapabilities = enabledCaps, DisabledCapabilities = disabledCaps,
            };
        }
    }

    public sealed class Nak : Cap
    {
        public required bool MoreComing { get; init; }
        public required IReadOnlySet<string> RejectedCapabilities { get; init; }

        public Nak(IrcMessage message) : base(message) { }

        public static Nak FromMessageInner(IrcMessage message)
        {
            var moreComing = message.Parameters[2] == "*";
            var caps = moreComing ? message.Parameters[3] : message.Parameters[2];

            return new Nak(message)
            {
                MoreComing = moreComing,
                RejectedCapabilities = new HashSet<string>(caps.Split(' ',
                    StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)),
            };
        }
    }

    public sealed class New : Cap
    {
        public required IReadOnlyDictionary<string, string?> AddedCapabilities { get; init; }

        public New(IrcMessage message) : base(message) { }

        public static New FromMessageInner(IrcMessage message)
        {
            var caps = new Dictionary<string, string?>();
            foreach (var cap in message.Parameters[2].Split(' ',
                         StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                var split = cap.Split('=', 2);
                if (split.Length == 0) caps[split[0]] = null;
                else caps[split[0]] = split[1];
            }

            return new New(message) { AddedCapabilities = caps };
        }
    }

    public sealed class Del : Cap
    {
        public required IReadOnlySet<string> RemovedCapabilities { get; init; }

        public Del(IrcMessage message) : base(message) { }

        public static Del FromMessageInner(IrcMessage message)
        {
            return new Del(message)
            {
                RemovedCapabilities = new HashSet<string>(message.Parameters[2].Split(' ',
                    StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)),
            };
        }
    }

    public sealed class Factory : ITypedMessageFactory
    {
        public string Command => "CAP";
        public IMessage FromMessage(IrcMessage message)
        {
            return message.Parameters[1].ToUpperInvariant() switch
            {
                "LS" => Ls.FromMessageInner(message),
                "LIST" => List.FromMessageInner(message),
                "ACK" => Ack.FromMessageInner(message),
                "NAK" => Nak.FromMessageInner(message),
                "NEW" => New.FromMessageInner(message),
                "DEL" => Del.FromMessageInner(message),
                _ => throw new NotImplementedException(),
            };
        }
    }
}

public sealed class Nick : TypedMessage
{
    public required string Nickname { get; init; }

    public Nick(IrcMessage message) : base(message) { }

    public sealed class Factory : ITypedMessageFactory
    {
        public string Command => "NICK";
        public IMessage FromMessage(IrcMessage message) => new Nick(message) { Nickname = message.Parameters[0] };
    }
}

public sealed class Authenticate : TypedMessage
{
    public const string EmptyPayload = "+";
    public required string Payload { get; init; }

    public Authenticate(IrcMessage message) : base(message) { }

    public sealed class Factory : ITypedMessageFactory
    {
        public string Command => "AUTHENTICATE";

        public IMessage FromMessage(IrcMessage message) => new Authenticate(message) { Payload = message.Parameters[0] };
    }
}

public sealed class LoggedIn : TypedMessage
{
    public required string Nickname { get; init; }
    public required string Ident { get; init; }
    public required string AccountName { get; init; }
    public required string Message { get; init; }

    public LoggedIn(IrcMessage message) : base(message) { }

    public sealed class Factory : ITypedMessageFactory
    {
        public string Command => "900"; // RPL_LOGGEDIN

        public IMessage FromMessage(IrcMessage message) => new LoggedIn(message)
        {
            Nickname = message.Parameters[0],
            Ident = message.Parameters[1],
            AccountName = message.Parameters[2],
            Message = message.Parameters[3],
        };
    }
}

public sealed class LoggedOut : TypedMessage
{
    public LoggedOut(IrcMessage message) : base(message) { }

    public sealed class Factory : ITypedMessageFactory
    {
        public string Command => "901"; // RPL_LOGGEDOUT
        public IMessage FromMessage(IrcMessage message) => new LoggedOut(message);
    }
}

public sealed class AccountLockedError : TypedMessage
{
    public required string Reason { get; init; }

    public AccountLockedError(IrcMessage message) : base(message) { }

    public sealed class Factory : ITypedMessageFactory
    {
        public string Command => "902"; // ERR_NICKLOCKED
        public IMessage FromMessage(IrcMessage message) => new AccountLockedError(message) { Reason = message.Parameters[1] };
    }
}

public sealed class SaslFailError : TypedMessage
{
    public required string Reason { get; init; }

    public SaslFailError(IrcMessage message) : base(message) { }

    public sealed class Factory : ITypedMessageFactory
    {
        public string Command => "904"; // ERR_SASLFAIL
        public IMessage FromMessage(IrcMessage message) => new SaslFailError(message) { Reason = message.Parameters[1] };
    }
}

public sealed class AlreadyRegisteredError : TypedMessage
{
    public AlreadyRegisteredError(IrcMessage message) : base(message) { }

    public sealed class Factory : ITypedMessageFactory
    {
        public string Command => "462"; // ERR_ALREADYREGISTERED
        public IMessage FromMessage(IrcMessage message) => new AlreadyRegisteredError(message);
    }
}

public sealed class IncorrectPasswordError : TypedMessage
{
    public IncorrectPasswordError(IrcMessage message) : base(message) { }

    public sealed class Factory : ITypedMessageFactory
    {
        public string Command => "464"; // ERR_PASSWDMISMATCH
        public IMessage FromMessage(IrcMessage message) => new IncorrectPasswordError(message);
    }
}

public sealed class ErroneousNicknameError : TypedMessage
{
    public required string Nickname { get; init; }
    public required string Reason { get; init; }

    public ErroneousNicknameError(IrcMessage message) : base(message) { }

    public sealed class Factory : ITypedMessageFactory
    {
        public string Command => "432"; // ERR_ERRONEUSNICKNAME
        public IMessage FromMessage(IrcMessage message) =>
            new ErroneousNicknameError(message) { Nickname = message.Parameters[1], Reason = message.Parameters[2] };
    }
}

public sealed class NicknameInUseError : TypedMessage
{
    public required string Nickname { get; init; }
    public required string Reason { get; init; }

    public NicknameInUseError(IrcMessage message) : base(message) { }

    public sealed class Factory : ITypedMessageFactory
    {
        public string Command => "433"; // ERR_NICKNAMEINUSE

        public IMessage FromMessage(IrcMessage message) =>
            new NicknameInUseError(message) { Nickname = message.Parameters[1], Reason = message.Parameters[2] };
    }
}

// TODO horsedocs mentions an ERR_NICKCOLLISION (436), with no further details

public static class RegistrationMessageFactory
{
    public static IrcMessage Authenticate(this IrcMessageFactory _, string mechanismOrPayload) =>
        IrcMessage.Create("AUTHENTICATE", mechanismOrPayload);
    public static IrcMessage CapEnd(this IrcMessageFactory _) => IrcMessage.Create("CAP", "END");
    public static IrcMessage CapLs(this IrcMessageFactory _) => IrcMessage.Create("CAP", "LS", "302");

    public static IrcMessage CapReq(this IrcMessageFactory _, IEnumerable<string> caps) =>
        IrcMessage.Create("CAP", "REQ", string.Join(' ', caps));
    public static IrcMessage Nick(this IrcMessageFactory _, string nick) => IrcMessage.Create("NICK", nick);
    public static IrcMessage Pass(this IrcMessageFactory _, string password) => IrcMessage.Create("PASS", password);

    public static IrcMessage User(this IrcMessageFactory _, string username, string realname) =>
        IrcMessage.Create("USER", username, "0", "*", realname);
}
