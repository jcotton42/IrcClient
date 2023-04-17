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
        client.State.Status = ClientStatus.NegotiatingCapabilities;
        client.ProcessCapabilityMetadata(availableCapabilities);
        var toReq = availableCapabilities.Keys.Intersect(SupportedCapabilities).ToHashSet();
        if (toReq.Contains("sasl") && client.ShouldAuthenticate())
        {
            client.State.WaitingOnSasl = true;
        }
        client.State.ReceivedAllCaps = !moreComing;
        if (toReq.Any()) client.SendMessageImmediately(IrcMessage.Factory.CapReq(toReq));
        if (client.State is { ReceivedAllCaps: true, WaitingOnSasl: false })
        {
            client.SendMessageImmediately(IrcMessage.Factory.CapEnd());
        }
    }

    // EnableCapabilities is what sends AUTHENTICATE
    // I **really** need to clean this up
    private void Ack(IrcClient client, Cap.Ack message) => client.EnableCapabilities(message.EnabledCapabilities);

    private void Nak(IrcClient client, Cap.Nak message)
    {
        if (message.RejectedCapabilities.Contains("sasl")) client.State.WaitingOnSasl = false;
        if (client.State is { ReceivedAllCaps: true, WaitingOnSasl: false })
        {
            client.SendMessageImmediately(IrcMessage.Factory.CapEnd());
        }
    }

    private void Del(IrcClient client, Cap.Del message) => client.DisableCapabilities(message.RemovedCapabilities);
}
