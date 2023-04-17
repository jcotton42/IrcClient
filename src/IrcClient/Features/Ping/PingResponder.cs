using IrcClient.Infrastructure;

namespace IrcClient.Features.Ping;

public class PingResponder : IResponder<Ping>
{
    public Task HandleAsync(IrcClient client, Ping message)
    {
        // PINGs are expected to be responded to ASAP
        client.SendMessageImmediately(IrcMessage.Factory.Pong(message.Token));
        return Task.CompletedTask;
    }
}
