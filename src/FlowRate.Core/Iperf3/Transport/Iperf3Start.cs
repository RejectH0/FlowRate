using System.Text.Json.Serialization;

namespace FlowRate.Core.Iperf3.Transport;

/// <summary>
/// The "start" section of iperf3 JSON output.
/// </summary>
public sealed class Iperf3Start
{
    [JsonPropertyName("connected")]
    public List<Iperf3Connection>? Connected { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("system_info")]
    public string? SystemInfo { get; set; }

    [JsonPropertyName("timestamp")]
    public Iperf3Timestamp? Timestamp { get; set; }

    [JsonPropertyName("connecting_to")]
    public Iperf3Endpoint? ConnectingTo { get; set; }

    [JsonPropertyName("cookie")]
    public string? Cookie { get; set; }

    [JsonPropertyName("tcp_mss_default")]
    public int? TcpMssDefault { get; set; }

    [JsonPropertyName("target_bitrate")]
    public long? TargetBitrate { get; set; }

    [JsonPropertyName("fq_rate")]
    public long? FqRate { get; set; }

    [JsonPropertyName("sock_bufsize")]
    public int? SockBufsize { get; set; }

    [JsonPropertyName("sndbuf_actual")]
    public int? SndbufActual { get; set; }

    [JsonPropertyName("rcvbuf_actual")]
    public int? RcvbufActual { get; set; }

    [JsonPropertyName("test_start")]
    public Iperf3TestStart? TestStart { get; set; }
}
