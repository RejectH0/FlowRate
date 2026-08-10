using System.Text.Json.Serialization;

namespace FlowRate.Core.Iperf3.Transport;

/// <summary>
/// End-of-test stream summary with both sender and receiver data.
/// </summary>
public sealed class Iperf3EndStream
{
    [JsonPropertyName("sender")]
    public Iperf3Stream? Sender { get; set; }

    [JsonPropertyName("receiver")]
    public Iperf3Stream? Receiver { get; set; }
}
