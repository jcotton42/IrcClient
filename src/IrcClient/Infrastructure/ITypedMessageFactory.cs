namespace IrcClient.Infrastructure;

public interface ITypedMessageFactory
{
    string Command { get; }
    IMessage FromMessage(IrcMessage message);
}
