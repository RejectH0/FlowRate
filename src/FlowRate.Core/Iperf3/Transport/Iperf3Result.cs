using System.Text.Json.Serialization;

namespace FlowRate.Core.Iperf3.Transport;

/// <summary>
/// Root JSON structure returned by iperf3.
/// Valid JSON may still represent a failure (check <see cref="Error"/>).
/// </summary>
public sealed class Iperf3Result
{
    [JsonPropertyName("start")]
    public Iperf3Start? Start { get; set; }

    [JsonPropertyName("intervals")]
    public List<Iperf3Interval>? Intervals { get; set; }

    [JsonPropertyName("end")]
    public Iperf3End? End { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// Returns true if this result represents a successful benchmark (no error).
    /// </summary>
    public bool IsSuccess => string.IsNullOrWhiteSpace(Error);
}
