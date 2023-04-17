using IrcClient.Infrastructure;

namespace IrcClient.Features.Registration;

public abstract class Cap : IFromMessage<Cap>
{
    public static string Command => "CAP";

    public static Cap FromMessage(IrcMessage message)
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

    public sealed class Ls : Cap
    {
        public required IReadOnlyDictionary<string, string?> AvailableCapabilities { get; init; }
        public required bool MoreComing { get; init; }

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

            return new Ls { AvailableCapabilities = caps, MoreComing = moreComing };
        }
    }

    public sealed class List : Cap
    {
        public required bool MoreComing { get; init; }
        public required IReadOnlySet<string> EnabledCapabilities { get; init; }

        public static List FromMessageInner(IrcMessage message)
        {
            var moreComing = message.Parameters[2] == "*";
            var caps = moreComing ? message.Parameters[3] : message.Parameters[2];
            return new List
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

            return new Ack
            {
                MoreComing = moreComing, EnabledCapabilities = enabledCaps, DisabledCapabilities = disabledCaps,
            };
        }
    }

    public sealed class Nak : Cap
    {
        public required bool MoreComing { get; init; }
        public required IReadOnlySet<string> RejectedCapabilities { get; init; }

        public static Nak FromMessageInner(IrcMessage message)
        {
            var moreComing = message.Parameters[2] == "*";
            var caps = moreComing ? message.Parameters[3] : message.Parameters[2];

            return new Nak
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

            return new New { AddedCapabilities = caps };
        }
    }

    public sealed class Del : Cap
    {
        public required IReadOnlySet<string> RemovedCapabilities { get; init; }

        public static Del FromMessageInner(IrcMessage message)
        {
            return new Del
            {
                RemovedCapabilities = new HashSet<string>(message.Parameters[2].Split(' ',
                    StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)),
            };
        }
    }
}

public sealed class Nick : IFromMessage<Nick>
{
    public required string Nickname { get; init; }
    public static string Command => "NICK";
    public static Nick FromMessage(IrcMessage message) => new() { Nickname = message.Parameters[0] };
}

public sealed class Authenticate : IFromMessage<Authenticate>
{
    public const string EmptyPayload = "+";
    public required string Payload { get; init; }

    public static string Command => "AUTHENTICATE";

    public static Authenticate FromMessage(IrcMessage message) => new() { Payload = message.Parameters[0] };
}

public sealed class LoggedIn : IFromMessage<LoggedIn>
{
    public required string Nickname { get; init; }
    public required string Ident { get; init; }
    public required string AccountName { get; init; }
    public required string Message { get; init; }

    public static string Command => "900"; // RPL_LOGGEDIN

    public static LoggedIn FromMessage(IrcMessage message) => new()
    {
        Nickname = message.Parameters[0],
        Ident = message.Parameters[1],
        AccountName = message.Parameters[2],
        Message = message.Parameters[3],
    };
}

public sealed class LoggedOut : IFromMessage<LoggedOut>
{
    public static string Command => "901"; // RPL_LOGGEDOUT
    public static LoggedOut FromMessage(IrcMessage message) => new();
}

public sealed class AccountLockedError : IFromMessage<AccountLockedError>
{
    public required string Reason { get; init; }

    public static string Command => "902"; // ERR_NICKLOCKED
    public static AccountLockedError FromMessage(IrcMessage message) => new() { Reason = message.Parameters[1] };
}

public sealed class SaslFailError : IFromMessage<SaslFailError>
{
    public required string Reason { get; init; }

    public static string Command => "904"; // ERR_SASLFAIL
    public static SaslFailError FromMessage(IrcMessage message) => new() { Reason = message.Parameters[1] };
}

public sealed class AlreadyRegisteredError : IFromMessage<AlreadyRegisteredError>
{
    public static string Command => "462"; // ERR_ALREADYREGISTERED
    public static AlreadyRegisteredError FromMessage(IrcMessage _) => new();
}

public sealed class IncorrectPasswordError : IFromMessage<IncorrectPasswordError>
{
    public static string Command => "464"; // ERR_PASSWDMISMATCH
    public static IncorrectPasswordError FromMessage(IrcMessage _) => new();
}

public sealed class ErroneousNicknameError : IFromMessage<ErroneousNicknameError>
{
    public required string Nickname { get; init; }
    public required string Reason { get; init; }

    public static string Command => "432"; // ERR_ERRONEUSNICKNAME

    public static ErroneousNicknameError FromMessage(IrcMessage message) =>
        new() { Nickname = message.Parameters[1], Reason = message.Parameters[2] };
}

public sealed class NicknameInUseError : IFromMessage<NicknameInUseError>
{
    public required string Nickname { get; init; }
    public required string Reason { get; init; }

    public static string Command => "433"; // ERR_NICKNAMEINUSE

    public static NicknameInUseError FromMessage(IrcMessage message) =>
        new() { Nickname = message.Parameters[1], Reason = message.Parameters[2] };
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
