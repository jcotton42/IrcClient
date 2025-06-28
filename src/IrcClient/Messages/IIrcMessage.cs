using System.Collections.Immutable;

namespace IrcClient.Messages;

public interface IIrcMessage
{
    public ImmutableDictionary<string, string?> Tags { get; }
    public IrcMessageSource? Source { get; }
    public string Command { get; }
}
