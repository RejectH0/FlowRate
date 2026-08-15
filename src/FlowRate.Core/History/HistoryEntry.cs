namespace FlowRate.Core.History;

/// <summary>
/// A lightweight, display-oriented record of a past benchmark run. The full
/// <see cref="FlowRate.Core.Domain.BenchmarkResult"/> is stored separately as JSON and
/// referenced by <see cref="FileName"/>.
/// </summary>
public sealed class HistoryEntry
{
    /// <summary>Stable identifier and JSON file name (without directory) for the stored run.</summary>
    public required string FileName { get; init; }

    /// <summary>When the run was recorded.</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>Server address the test targeted.</summary>
    public string? Server { get; init; }

    /// <summary>Server port.</summary>
    public int? Port { get; init; }

    /// <summary>Protocol used (TCP/UDP).</summary>
    public string? Protocol { get; init; }

    /// <summary>Effective throughput in Mbps for quick display.</summary>
    public double EffectiveMbps { get; init; }

    /// <summary>Whether the run succeeded.</summary>
    public bool IsSuccess { get; init; }

    /// <summary>Convenience label for list display.</summary>
    public string DisplayText =>
        $"{Timestamp.LocalDateTime:g}  \u2022  {Server}  \u2022  {EffectiveMbps:F0} Mbps";
}
