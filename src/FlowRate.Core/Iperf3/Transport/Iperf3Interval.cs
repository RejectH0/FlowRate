using System.Text.Json.Serialization;

namespace FlowRate.Core.Iperf3.Transport;

/// <summary>
/// A single interval snapshot during the test.
/// </summary>
public sealed class Iperf3Interval
{
    [JsonPropertyName("streams")]
    public List<Iperf3Stream>? Streams { get; set; }

    [JsonPropertyName("sum")]
    public Iperf3Stream? Sum { get; set; }
}
