namespace IrcClient.Infrastructure;

public abstract class TypedMessage : IMessage
{
    public string Command { get; }
    public string? Source { get; }
    public IReadOnlyDictionary<string, string?> Tags { get; }
    public IReadOnlyList<string> Parameters { get; }

    protected TypedMessage(IrcMessage message)
    {
        Command = message.Command;
        Source = message.Source;
        Tags = message.Tags;
        Parameters = message.Parameters;
    }
}
