namespace IrcClient.Tests.Data;

public sealed class MsgSplit
{
    public required string Input { get; init; }
    public required MsgSplitAtoms Atoms { get; init; }

    public override string ToString() => Input;
}

public sealed class MsgSplitAtoms
{
    public Dictionary<string, string?>? Tags { get; init; }
    public string? Source { get; init; }
    public required string Verb { get; init; }
    public string[]? Params { get; init; }
}
