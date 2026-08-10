namespace FlowRate.Core.Domain;

/// <summary>
/// A snapshot of throughput during a single time interval.
/// </summary>
public sealed class IntervalSnapshot
{
    public required int IntervalNumber { get; init; }
    public required ThroughputMetrics Aggregate { get; init; }
    public IReadOnlyList<ThroughputMetrics>? PerStreamMetrics { get; init; }
}
