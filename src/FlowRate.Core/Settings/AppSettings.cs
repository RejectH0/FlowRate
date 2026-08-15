namespace FlowRate.Core.Settings;

/// <summary>
/// A named, savable set of benchmark configuration values.
/// </summary>
public sealed class BenchmarkProfile
{
    public string Name { get; set; } = string.Empty;
    public string ServerAddress { get; set; } = string.Empty;
    public int Port { get; set; } = 5201;
    public int DurationSeconds { get; set; } = 10;
    public int ParallelStreams { get; set; } = 1;
    public bool ReverseMode { get; set; }
    public bool UdpMode { get; set; }

    /// <summary>Target bitrate in Mbps (0 = unlimited / iperf3 default).</summary>
    public double TargetBitrateMbps { get; set; }

    /// <summary>TCP window / socket buffer size in KB (0 = iperf3 default).</summary>
    public int WindowSizeKB { get; set; }
}

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

    /// <summary>Default UDP mode (vs TCP) setting.</summary>
    public bool UdpMode { get; set; }

    /// <summary>Default target bitrate in Mbps (0 = unlimited / iperf3 default).</summary>
    public double TargetBitrateMbps { get; set; }

    /// <summary>Default TCP window / socket buffer size in KB (0 = iperf3 default).</summary>
    public int WindowSizeKB { get; set; }

    /// <summary>Saved named configuration profiles.</summary>
    public List<BenchmarkProfile> Profiles { get; set; } = new();

    /// <summary>Recently used server addresses, most recent first.</summary>
    public List<string> RecentServers { get; set; } = new();
}
