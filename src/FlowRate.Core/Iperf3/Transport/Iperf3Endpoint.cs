using System.Text.Json.Serialization;

namespace FlowRate.Core.Iperf3.Transport;

/// <summary>
/// Simple host/port pair for connecting_to.
/// </summary>
public sealed class Iperf3Endpoint
{
    [JsonPropertyName("host")]
    public string? Host { get; set; }

    [JsonPropertyName("port")]
    public int Port { get; set; }
}
