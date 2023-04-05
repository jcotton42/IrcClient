using YamlDotNet.Serialization;

namespace IrcClient.Tests;

public sealed class ToStringTestDataRoot
{
    public required ToStringTestData[] Tests { get; init; }
}

public sealed class ToStringTestData
{
    [YamlMember(Alias = "desc")]
    public required string Description { get; init; }
    public required ToStringTestAtoms Atoms { get; init; }
    public required string[] Matches { get; init; }
}

public sealed class ToStringTestAtoms
{
    public Dictionary<string, string?>? Tags { get; init; }
    public string? Source { get; init; }
    public required string Verb { get; init; }
    public string[]? Params { get; init; }
}
