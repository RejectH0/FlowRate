using System.Text.Json.Serialization;

namespace FlowRate.Core.Iperf3.Transport;

/// <summary>
/// The "end" section of iperf3 output containing final summary.
/// </summary>
public sealed class Iperf3End
{
    [JsonPropertyName("streams")]
    public List<Iperf3EndStream>? Streams { get; set; }

    [JsonPropertyName("sum_sent")]
    public Iperf3Stream? SumSent { get; set; }

    [JsonPropertyName("sum_received")]
    public Iperf3Stream? SumReceived { get; set; }

    /// <summary>
    /// UDP tests report a single aggregate "sum" object (carrying jitter and packet-loss)
    /// instead of the TCP sum_sent/sum_received pair.
    /// </summary>
    [JsonPropertyName("sum")]
    public Iperf3Stream? Sum { get; set; }

    [JsonPropertyName("cpu_utilization_percent")]
    public Iperf3CpuUtilization? CpuUtilizationPercent { get; set; }

    [JsonPropertyName("receiver_tcp_congestion")]
    public string? ReceiverTcpCongestion { get; set; }
}
