using FluentAssertions;
using FluentAssertions.Execution;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace IrcClient.Tests;

public sealed class IrcMessageParserTests
{
    public static TheoryData<ParseTestData> ParseTestData { get; }
    public static TheoryData<ToStringTestData> ToStringTestData { get; }

    static IrcMessageParserTests()
    {
        var deserializer = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build();
        using var parseData = File.OpenText(Path.Combine(AppContext.BaseDirectory, "Data", "parse-data.yaml"));
        using var toStringData = File.OpenText(Path.Combine(AppContext.BaseDirectory, "Data", "tostring-data.yaml"));

        ParseTestData = new TheoryData<ParseTestData>();
        foreach (var test in deserializer.Deserialize<ParseTestDataRoot>(parseData).Tests) ParseTestData.Add(test);
        ToStringTestData = new TheoryData<ToStringTestData>();
        foreach (var test in deserializer.Deserialize<ToStringTestDataRoot>(toStringData).Tests) ToStringTestData.Add(test);
    }

    [MemberData(nameof(ParseTestData))]
    [Theory]
    public void Test_Parser(ParseTestData data)
    {
        var message = IrcMessage.Parse(data.Input);
        using (new AssertionScope())
        {
            message.Source.Should().Be(data.Atoms.Source);
            message.Command.Should().Be(data.Atoms.Verb);

            if (data.Atoms.Params is null) message.Parameters.Should().BeEmpty();
            else message.Parameters.Should().Equal(data.Atoms.Params);

            if (data.Atoms.Tags is null) message.Tags.Should().BeEmpty();
            else message.Tags.Should().Equal(data.Atoms.Tags);
        }
    }

    [MemberData(nameof(ToStringTestData))]
    [Theory]
    public void Test_ToString(ToStringTestData data)
    {
        var message = new IrcMessage
        {
            Tags = data.Atoms.Tags ?? new Dictionary<string, string?>(),
            Source = data.Atoms.Source,
            Command = data.Atoms.Verb,
            Parameters = data.Atoms.Params ?? Array.Empty<string>(),
        };

        message.ToString().Should().BeOneOf(data.Matches);
    }
}
