namespace IrcClient.Infrastructure;

public interface IResponder<in T> where T : IMessage
{
    Task HandleAsync(IrcClient client, T message, CancellationToken ct = default);
}
