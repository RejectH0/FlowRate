namespace FlowRate.Core.Settings;

/// <summary>
/// User-configurable application preferences that persist across sessions.
/// </summary>
public sealed class AppSettings
{
    /// <summary>Default iperf3 server address pre-filled on launch.</summary>
    public string ServerAddress { get; set; } = "192.168.1.100";

    /// <summary>Default iperf3 server port.</summary>
    public int Port { get; set; } = 5201;

    /// <summary>Default test duration in seconds.</summary>
    public int DurationSeconds { get; set; } = 10;

    /// <summary>Default number of parallel streams.</summary>
    public int ParallelStreams { get; set; } = 1;

    /// <summary>Default reverse-mode (server sends) setting.</summary>
    public bool ReverseMode { get; set; }

    /// <summary>Whether the live feed keeps all intervals by default.</summary>
    public bool ShowAllIntervals { get; set; } = true;
}
