using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace IrcClient.Messages;

[IrcMessage("JOIN")]
public sealed partial record JoinMessage(
    [IrcParameter(ListDelimiter = ',')] ImmutableArray<string> Channels,
    [IrcParameter(ListDelimiter = ',')] ImmutableArray<string> Keys)
{
    public JoinMessage([IrcParameter(ListDelimiter = ',')] ImmutableArray<string> channels)
        : this(channels, ImmutableArray<string>.Empty)
    {
    }
}

public sealed partial record JoinMessage
{
    // if eg Split() ends up being used, see ImmutableCollectionsMarshal for a cheap conversion from T[] to ImmutableArray<T>
    public static JoinMessage FromRaw(RawIrcMessage raw) => raw.Parameters.Length switch
    {
        2 => new JoinMessage(
            ImmutableCollectionsMarshal.AsImmutableArray(raw.Parameters[0].Split(',')),
            ImmutableCollectionsMarshal.AsImmutableArray(raw.Parameters[1].Split(','))),
        1 => new JoinMessage(
            ImmutableCollectionsMarshal.AsImmutableArray(raw.Parameters[0].Split(','))),
        _ => throw null,
    };
}
