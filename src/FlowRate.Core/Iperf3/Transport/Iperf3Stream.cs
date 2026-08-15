using System.Text.Json.Serialization;

namespace FlowRate.Core.Iperf3.Transport;

/// <summary>
/// Per-stream throughput data for an interval or end summary.
/// </summary>
public sealed class Iperf3Stream
{
    [JsonPropertyName("socket")]
    public int? Socket { get; set; }

    [JsonPropertyName("start")]
    public double Start { get; set; }

    [JsonPropertyName("end")]
    public double End { get; set; }

    [JsonPropertyName("seconds")]
    public double Seconds { get; set; }

    [JsonPropertyName("bytes")]
    public long Bytes { get; set; }

    [JsonPropertyName("bits_per_second")]
    public double BitsPerSecond { get; set; }

    [JsonPropertyName("omitted")]
    public bool Omitted { get; set; }

    [JsonPropertyName("sender")]
    public bool Sender { get; set; }

    // --- UDP-only fields (present when the test protocol is UDP) ---

    [JsonPropertyName("jitter_ms")]
    public double? JitterMs { get; set; }

    [JsonPropertyName("lost_packets")]
    public long? LostPackets { get; set; }

    [JsonPropertyName("packets")]
    public long? Packets { get; set; }

    [JsonPropertyName("lost_percent")]
    public double? LostPercent { get; set; }
}
