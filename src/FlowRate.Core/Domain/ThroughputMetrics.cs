namespace FlowRate.Core.Domain;

/// <summary>
/// Throughput metrics for a time interval or final summary.
/// </summary>
public sealed class ThroughputMetrics
{
    public required double StartSeconds { get; init; }
    public required double EndSeconds { get; init; }
    public required double DurationSeconds { get; init; }

    public required long Bytes { get; init; }
    public required double BitsPerSecond { get; init; }

    /// <summary>
    /// Convenience property: throughput in megabits per second.
    /// </summary>
    public double Mbps => BitsPerSecond / 1_000_000.0;

    /// <summary>
    /// Convenience property: throughput in gigabits per second.
    /// </summary>
    public double Gbps => BitsPerSecond / 1_000_000_000.0;

    /// <summary>
    /// Convenience property: total data in megabytes.
    /// </summary>
    public double MegaBytes => Bytes / 1_048_576.0;

    /// <summary>
    /// Convenience property: total data in gigabytes.
    /// </summary>
    public double GigaBytes => Bytes / 1_073_741_824.0;
}
