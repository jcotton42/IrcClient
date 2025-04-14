using System.Collections.ObjectModel;
using IrcClient.Messages;
using IrcClient.Tests.Data;
using Shouldly;

namespace IrcClient.Tests;

public sealed class IrcMessageTests
{
    [Test]
    [MessageData<MsgJoin>("msg-join.yaml")]
    [DisplayName("$data")]
    public void MessageToString(MsgJoin data)
    {
        var message = new RawIrcMessage
        {
            Tags = data.Atoms.Tags is not null
                ? new ReadOnlyDictionary<string, string?>(data.Atoms.Tags)
                : ReadOnlyDictionary<string, string?>.Empty,
            Source = data.Atoms.Source is not null ? IrcMessageSource.Parse(data.Atoms.Source) : null,
            Command = data.Atoms.Verb,
            Parameters = data.Atoms.Params is not null
                ? new ReadOnlyCollection<string>(data.Atoms.Params)
                : ReadOnlyCollection<string>.Empty,
        };

        var text = message.ToString();
        text.ShouldBeOneOf(data.Matches);
    }

    [Test]
    [MessageData<MsgSplit>("msg-split.yaml")]
    [DisplayName("$data")]
    public void MessageParsing(MsgSplit data)
    {
        var message = RawIrcMessage.Parse(data.Input);
        message.Tags.ShouldBe(data.Atoms.Tags?.AsReadOnly() ?? ReadOnlyDictionary<string, string?>.Empty);
        message.Source?.ToString().ShouldBe(data.Atoms.Source);
        message.Command.ShouldBe(data.Atoms.Verb);
        message.Parameters.ShouldBe(data.Atoms.Params ?? []);
    }

    [Test]
    [MessageData<UserHostSplit>("userhost-split.yaml")]
    [DisplayName("$data")]
    public void SourceParsing(UserHostSplit data)
    {
        var source = IrcMessageSource.Parse(data.Source);
        source.ServerOrNick.ShouldBe(data.Atoms.Nick);
        source.User.ShouldBe(data.Atoms.User);
        source.Host.ShouldBe(data.Atoms.Host);
    }
}
