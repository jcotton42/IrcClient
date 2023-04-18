namespace IrcClient.Infrastructure;

public interface IMessage
{
    public string Command { get; }
    public string? Source { get; }
    public IReadOnlyDictionary<string, string?> Tags { get; }
    public IReadOnlyList<string> Parameters { get; }
}
