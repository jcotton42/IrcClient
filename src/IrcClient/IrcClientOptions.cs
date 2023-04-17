namespace IrcClient;

public sealed class IrcClientOptions
{
    public required string Hostname { get; init; }
    public required int Port { get; init; }
    public required bool UseSsl { get; init; }
    public string? Password { get; init; }
    public required string Nick { get; init; }
    public required string Username { get; init; }
    public required string Realname { get; init; }

    public SaslPlain? SaslPlain { get; init; }
}

public sealed class SaslPlain
{
    public required string Username { get; init; }
    public required string Password { get; init; }
}
