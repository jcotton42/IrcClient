namespace IrcClient.Tests;

public sealed class ParseTestDataRoot
{
    public required ParseTestData[] Tests { get; init; }
}

public sealed class ParseTestData
{
    public required string Input { get; init; }
    public required ParseTestAtoms Atoms { get; init; }
}

public sealed class ParseTestAtoms
{
    public Dictionary<string, string?>? Tags { get; init; }
    public string? Source { get; init; }
    public required string Verb { get; init; }
    public string[]? Params { get; init; }
}
