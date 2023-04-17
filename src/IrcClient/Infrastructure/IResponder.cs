namespace IrcClient.Infrastructure;

public interface IResponder<T> where T : IFromMessage<T>
{
    Task HandleAsync(IrcClient client, T message);
}
