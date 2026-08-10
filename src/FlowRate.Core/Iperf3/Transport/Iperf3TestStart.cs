using System.Text.Json.Serialization;

namespace FlowRate.Core.Iperf3.Transport;

/// <summary>
/// The "test_start" section describing the test parameters.
/// </summary>
public sealed class Iperf3TestStart
{
    [JsonPropertyName("protocol")]
    public string? Protocol { get; set; }

    [JsonPropertyName("num_streams")]
    public int NumStreams { get; set; }

    [JsonPropertyName("blksize")]
    public int Blksize { get; set; }

    [JsonPropertyName("omit")]
    public int Omit { get; set; }

    [JsonPropertyName("duration")]
    public int Duration { get; set; }

    [JsonPropertyName("bytes")]
    public long Bytes { get; set; }

    [JsonPropertyName("blocks")]
    public long Blocks { get; set; }

    /// <summary>
    /// 0 = forward (client sends), 1 = reverse (server sends).
    /// </summary>
    [JsonPropertyName("reverse")]
    public int Reverse { get; set; }

    [JsonPropertyName("tos")]
    public int Tos { get; set; }

    [JsonPropertyName("target_bitrate")]
    public long TargetBitrate { get; set; }

    [JsonPropertyName("bidir")]
    public int Bidir { get; set; }

    [JsonPropertyName("fqrate")]
    public long Fqrate { get; set; }

    [JsonPropertyName("interval")]
    public int Interval { get; set; }

    [JsonPropertyName("gso")]
    public int Gso { get; set; }

    [JsonPropertyName("gro")]
    public int Gro { get; set; }
}
