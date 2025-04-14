namespace IrcClient.Tests.Data;

public sealed class MsgJoin
{
    public required string Desc { get; init; }
    public required MsgJoinAtoms Atoms { get; init; }
    public required string[] Matches { get; init; }

    public override string ToString() => Desc;
}

public sealed class MsgJoinAtoms
{
    public Dictionary<string, string>? Tags { get; init; }
    public string? Source { get; init; }
    public required string Verb { get; init; }
    public string[]? Params { get; init; }
}
