using System.Text.Json.Serialization;

namespace FlowRate.Core.Iperf3.Transport;

/// <summary>
/// Represents a single TCP/UDP connection in the "connected" array.
/// </summary>
public sealed class Iperf3Connection
{
    [JsonPropertyName("socket")]
    public int Socket { get; set; }

    [JsonPropertyName("local_host")]
    public string? LocalHost { get; set; }

    [JsonPropertyName("local_port")]
    public int LocalPort { get; set; }

    [JsonPropertyName("remote_host")]
    public string? RemoteHost { get; set; }

    [JsonPropertyName("remote_port")]
    public int RemotePort { get; set; }
}
