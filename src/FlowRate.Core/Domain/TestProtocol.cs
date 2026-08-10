namespace FlowRate.Core.Domain;

/// <summary>
/// The transport protocol used for the benchmark.
/// </summary>
public enum TestProtocol
{
    /// <summary>
    /// Transmission Control Protocol.
    /// </summary>
    Tcp,

    /// <summary>
    /// User Datagram Protocol.
    /// </summary>
    Udp,

    /// <summary>
    /// Stream Control Transmission Protocol.
    /// </summary>
    Sctp
}
