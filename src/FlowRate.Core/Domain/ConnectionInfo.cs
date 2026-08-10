namespace FlowRate.Core.Domain;

/// <summary>
/// Information about a network connection endpoint.
/// </summary>
public sealed class ConnectionInfo
{
    public required string LocalHost { get; init; }
    public required int LocalPort { get; init; }
    public required string RemoteHost { get; init; }
    public required int RemotePort { get; init; }

    public override string ToString() =>
        $"{LocalHost}:{LocalPort} → {RemoteHost}:{RemotePort}";
}
