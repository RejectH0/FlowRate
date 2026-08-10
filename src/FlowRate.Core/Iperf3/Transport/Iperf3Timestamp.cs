using System.Text.Json.Serialization;

namespace FlowRate.Core.Iperf3.Transport;

/// <summary>
/// Timestamp metadata from iperf3 start.
/// </summary>
public sealed class Iperf3Timestamp
{
    [JsonPropertyName("time")]
    public string? Time { get; set; }

    [JsonPropertyName("timesecs")]
    public long Timesecs { get; set; }

    [JsonPropertyName("timemillisecs")]
    public long Timemillisecs { get; set; }
}
