namespace IrcClient.Messages;

public interface IFromMessage<T> where T : IFromMessage<T>
{
    static abstract string Command { get; }
    static abstract T FromMessage(IrcMessage message);
}
