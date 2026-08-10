namespace FlowRate.Core.Domain;

/// <summary>
/// CPU utilization metrics for local and remote hosts during a benchmark.
/// All values are percentages (0-100).
/// </summary>
public sealed class CpuUtilization
{
    public required double LocalTotal { get; init; }
    public required double LocalUser { get; init; }
    public required double LocalSystem { get; init; }

    public required double RemoteTotal { get; init; }
    public required double RemoteUser { get; init; }
    public required double RemoteSystem { get; init; }
}
