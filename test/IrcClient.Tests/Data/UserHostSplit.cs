namespace IrcClient.Tests.Data;

public sealed class UserHostSplit
{
    public required string Source { get; init; }
    public required UserHostSplitAtoms Atoms { get; init; }

    public override string ToString() => Source;
}

public sealed class UserHostSplitAtoms
{
    public required string Nick { get; init; }
    public string? User { get; init; }
    public string? Host { get; init; }
}
