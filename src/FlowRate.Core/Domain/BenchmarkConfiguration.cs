namespace FlowRate.Core.Domain;

/// <summary>
/// Configuration parameters for a benchmark test.
/// </summary>
public sealed class BenchmarkConfiguration
{
    public required TestProtocol Protocol { get; init; }
    public required Direction Direction { get; init; }

    public required int StreamCount { get; init; }
    public required int DurationSeconds { get; init; }
    public required int IntervalSeconds { get; init; }

    public required int BlockSize { get; init; }
    public required int OmitSeconds { get; init; }

    public long? TargetBitrate { get; init; }
    public int? TypeOfService { get; init; }

    public string? RemoteHost { get; init; }
    public int? RemotePort { get; init; }
}
