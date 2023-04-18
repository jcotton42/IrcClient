using System.Buffers.Text;
using System.Diagnostics;
using System.Text;

using IrcClient.Infrastructure;

namespace IrcClient.Features.Registration;

public sealed class RegistrationResponder : IResponder<Cap>, IResponder<Authenticate>
{
    private readonly static HashSet<string> SupportedCapabilities = new() { "cap-notify", "sasl" };

    public Task HandleAsync(IrcClient client, Cap message, CancellationToken ct)
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

    public Task HandleAsync(IrcClient client, Authenticate message, CancellationToken ct)
    {
        // TODO right now PLAIN is assumed, eventually this will need to be updated for that
        // I'm thinking a discriminated union of the various SASL options?
        // That will also take care of the use of ! below
        Debug.Assert(message.Payload == Authenticate.EmptyPayload, "SASL PLAIN starts with an empty server payload");
        var username = client.Options.SaslPlain!.Username;
        var password = client.Options.SaslPlain!.Password;
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}\0{username}\0{password}"));

        var lastWas400 = false;
        for (var offset = 0; offset < payload.Length; offset += 400)
        {
            var chunk = payload[offset..Math.Min(offset + 400, payload.Length)];
            lastWas400 = chunk.Length == 400;
            client.SendMessageImmediately(IrcMessage.Factory.Authenticate(chunk));
        }
        if (lastWas400) client.SendMessageImmediately(IrcMessage.Factory.Authenticate("+"));
        return Task.CompletedTask;
    }
}
