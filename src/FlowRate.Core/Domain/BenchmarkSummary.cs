namespace FlowRate.Core.Domain;

/// <summary>
/// Final summary of a completed benchmark test.
/// </summary>
public sealed class BenchmarkSummary
{
    public required ThroughputMetrics Sent { get; init; }
    public required ThroughputMetrics Received { get; init; }

    public CpuUtilization? CpuUtilization { get; init; }
    public string? TcpCongestionAlgorithm { get; init; }

    /// <summary>
    /// UDP aggregate jitter/packet-loss summary, when the test protocol is UDP.
    /// Null for TCP tests.
    /// </summary>
    public ThroughputMetrics? Udp { get; init; }

    /// <summary>
    /// The effective throughput (typically the receiver's measurement).
    /// </summary>
    public double EffectiveBitsPerSecond => Received.BitsPerSecond;

    /// <summary>
    /// The effective throughput in Gbps.
    /// </summary>
    public double EffectiveGbps => Received.Gbps;

    /// <summary>
    /// The effective throughput in Mbps.
    /// </summary>
    public double EffectiveMbps => Received.Mbps;
}
