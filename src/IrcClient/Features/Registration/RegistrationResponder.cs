using IrcClient.Infrastructure;

namespace IrcClient.Features.Registration;

public sealed class RegistrationResponder : IResponder<Cap>
{
    private readonly static HashSet<string> SupportedCapabilities = new() { "cap-notify", "sasl" };

    public Task HandleAsync(IrcClient client, Cap message)
    {
        switch (message)
        {
            case Cap.Ls ls:
                LsOrNew(client, ls.AvailableCapabilities, ls.MoreComing);
                break;
            case Cap.Ack ack:
                Ack(client, ack);
                break;
            case Cap.Nak nak:
                Nak(client, nak);
                break;
            case Cap.New @new:
                LsOrNew(client, @new.AddedCapabilities, moreComing: false);
                break;
            case Cap.Del del:
                Del(client, del);
                break;
            default:
                throw new NotSupportedException();
        }
        return Task.CompletedTask;
    }

    private void LsOrNew(IrcClient client, IReadOnlyDictionary<string, string?> availableCapabilities, bool moreComing)
    {
        client.ProcessCapabilityMetadata(availableCapabilities);
        var toReq = availableCapabilities.Keys.Intersect(SupportedCapabilities);
        client.SendMessageImmediately(IrcMessage.Factory.CapReq(toReq));
        // TODO if more coming/not coming?
    }

    private void Ack(IrcClient client, Cap.Ack message) => client.EnableCapabilities(message.EnabledCapabilities);

    private void Nak(IrcClient client, Cap.Nak message)
    {
        // TODO
    }

    private void Del(IrcClient client, Cap.Del message) => client.DisableCapabilities(message.RemovedCapabilities);
}
