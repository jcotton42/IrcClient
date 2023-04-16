namespace IrcClient.Messages;

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
        public required IReadOnlyList<string> EnabledCapabilities { get; init; }

        public static List FromMessageInner(IrcMessage message)
        {
            var moreComing = message.Parameters[2] == "*";
            var caps = moreComing ? message.Parameters[3] : message.Parameters[2];
            return new List
            {
                MoreComing = moreComing,
                EnabledCapabilities = caps.Split(' ',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            };
        }
    }

    public sealed class Ack : Cap
    {
        public required bool MoreComing { get; init; }
        public required IReadOnlyList<string> EnabledCapabilities { get; init; }
        public required IReadOnlyList<string> DisabledCapabilities { get; init; }

        public static Ack FromMessageInner(IrcMessage message)
        {
            var moreComing = message.Parameters[2] == "*";
            var caps = moreComing ? message.Parameters[3] : message.Parameters[2];
            var enabledCaps = new List<string>();
            var disabledCaps = new List<string>();

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
        public required IReadOnlyList<string> RejectedCapabilities { get; init; }

        public static Nak FromMessageInner(IrcMessage message)
        {
            var moreComing = message.Parameters[2] == "*";
            var caps = moreComing ? message.Parameters[3] : message.Parameters[2];

            return new Nak
            {
                MoreComing = moreComing,
                RejectedCapabilities = caps.Split(' ',
                    StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
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
        public required IReadOnlyList<string> RemovedCapabilities { get; init; }

        public static Del FromMessageInner(IrcMessage message)
        {
            return new Del
            {
                RemovedCapabilities = message.Parameters[2].Split(' ',
                    StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
            };
        }
    }
}

public static class CapMessageFactory
{
    public static IrcMessage CapEnd(this IrcMessageFactory _) => IrcMessage.Create("CAP", "END");
    public static IrcMessage CapLs(this IrcMessageFactory _) => IrcMessage.Create("CAP", "LS", "302");

    public static IrcMessage CapReq(this IrcMessageFactory _, params string[] caps) =>
        IrcMessage.Create("CAP", "REQ", string.Join(' ', caps));
    // TODO move these
    public static IrcMessage Nick(this IrcMessageFactory _, string nick) => IrcMessage.Create("NICK", nick);
    public static IrcMessage Pass(this IrcMessageFactory _, string password) => IrcMessage.Create("PASS", password);

    public static IrcMessage User(this IrcMessageFactory _, string username, string realname) =>
        IrcMessage.Create("USER", username, "0", "*", realname);
}
