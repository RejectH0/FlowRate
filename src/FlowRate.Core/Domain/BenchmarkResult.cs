namespace FlowRate.Core.Domain;

/// <summary>
/// The result of a completed benchmark test.
/// Check <see cref="IsSuccess"/> to determine if the test succeeded.
/// </summary>
public sealed class BenchmarkResult
{
    /// <summary>
    /// True if the benchmark completed successfully.
    /// </summary>
    public required bool IsSuccess { get; init; }

    /// <summary>
    /// Error message if the benchmark failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// The iperf3 version that produced this result.
    /// </summary>
    public string? IperfVersion { get; init; }

    /// <summary>
    /// When the test started (UTC).
    /// </summary>
    public DateTimeOffset? StartTime { get; init; }

    /// <summary>
    /// Test configuration parameters.
    /// </summary>
    public BenchmarkConfiguration? Configuration { get; init; }

    /// <summary>
    /// Connection information for the first stream.
    /// </summary>
    public ConnectionInfo? Connection { get; init; }

    /// <summary>
    /// Interval snapshots collected during the test.
    /// </summary>
    public IReadOnlyList<IntervalSnapshot>? Intervals { get; init; }

    /// <summary>
    /// Final test summary (only present if test succeeded).
    /// </summary>
    public BenchmarkSummary? Summary { get; init; }
}
