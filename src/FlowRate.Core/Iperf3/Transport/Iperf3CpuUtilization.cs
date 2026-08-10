using System.Text.Json.Serialization;

namespace FlowRate.Core.Iperf3.Transport;

/// <summary>
/// CPU utilization metrics from both local and remote hosts.
/// </summary>
public sealed class Iperf3CpuUtilization
{
    [JsonPropertyName("host_total")]
    public double HostTotal { get; set; }

    [JsonPropertyName("host_user")]
    public double HostUser { get; set; }

    [JsonPropertyName("host_system")]
    public double HostSystem { get; set; }

    [JsonPropertyName("remote_total")]
    public double RemoteTotal { get; set; }

    [JsonPropertyName("remote_user")]
    public double RemoteUser { get; set; }

    [JsonPropertyName("remote_system")]
    public double RemoteSystem { get; set; }
}
