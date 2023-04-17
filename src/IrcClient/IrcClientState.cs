namespace IrcClient;

public sealed class IrcClientState
{
    public ClientStatus Status { get; internal set; } = ClientStatus.Disconnected;
    public bool SaslAvailable { get; internal set; } = false;
    public string[]? SupportedSaslMechanisms { get; internal set; }
}

public enum ClientStatus
{
    Disconnected,
    Registering,
    Connected,
}
