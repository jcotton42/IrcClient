using System.Collections.ObjectModel;
using IrcClient.Messages;
using IrcClient.Tests.Data;
using Shouldly;

namespace IrcClient.Tests;

public sealed class IrcMessageTests
{
    [Test]
    [MessageData<MsgJoin>("msg-join.yaml")]
    public void Parsing(MsgJoin data)
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
}
