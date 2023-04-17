namespace IrcClient;

public sealed class IrcClientState
{
    public ClientStatus Status { get; internal set; } = ClientStatus.Disconnected;
    public bool IsSaslAvailable { get; internal set; } = false;
    public bool ReceivedAllCaps { get; internal set; }
    public string[]? SupportedSaslMechanisms { get; internal set; }
    public bool WaitingOnSasl { get; internal set; }
}

public enum ClientStatus
{
    Disconnected,
    Registering,
    NegotiatingCapabilities,
    Authenticating,
    Connected,
}
