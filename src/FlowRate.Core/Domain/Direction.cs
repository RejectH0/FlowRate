namespace FlowRate.Core.Domain;

/// <summary>
/// The direction of data flow in a benchmark test.
/// </summary>
public enum Direction
{
    /// <summary>
    /// Client sends data to server (iperf3 default, reverse=0).
    /// </summary>
    Forward = 0,

    /// <summary>
    /// Server sends data to client (iperf3 -R flag, reverse=1).
    /// </summary>
    Reverse = 1,

    /// <summary>
    /// Bidirectional test (iperf3 --bidir flag).
    /// </summary>
    Bidirectional = 2
}
