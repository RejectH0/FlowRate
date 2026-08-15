using FlowRate.Core.Domain;
using FlowRate.Core.Iperf3;

namespace FlowRate.Core.Tests.Iperf3;

/// <summary>
/// Tests for Iperf3Parser using real fixture files.
/// </summary>
public class Iperf3ParserTests
{
    private readonly Iperf3Parser _parser = new();

    [Fact]
    public void Parse_SingleStreamForward_Success()
    {
        // Arrange
        var json = LoadFixture("flowrate-iperf3-single-stream.json");

        // Act
        var result = _parser.Parse(json);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.ErrorMessage);
        Assert.Equal("iperf 3.21", result.IperfVersion);

        // Configuration
        Assert.NotNull(result.Configuration);
        Assert.Equal(TestProtocol.Tcp, result.Configuration.Protocol);
        Assert.Equal(Direction.Forward, result.Configuration.Direction);
        Assert.Equal(1, result.Configuration.StreamCount);
        Assert.Equal(10, result.Configuration.DurationSeconds);
        Assert.Equal(1, result.Configuration.IntervalSeconds);

        // Connection
        Assert.NotNull(result.Connection);
        Assert.Equal("10.20.65.145", result.Connection.LocalHost);
        Assert.Equal(54408, result.Connection.LocalPort);
        Assert.Equal("10.20.65.160", result.Connection.RemoteHost);
        Assert.Equal(5201, result.Connection.RemotePort);

        // Intervals
        Assert.NotNull(result.Intervals);
        Assert.Equal(10, result.Intervals.Count);

        var firstInterval = result.Intervals[0];
        Assert.Equal(1, firstInterval.IntervalNumber);
        Assert.NotNull(firstInterval.Aggregate);
        Assert.True(firstInterval.Aggregate.BitsPerSecond > 0);
        Assert.True(firstInterval.Aggregate.Gbps > 0);

        // Summary
        Assert.NotNull(result.Summary);
        Assert.NotNull(result.Summary.Sent);
        Assert.NotNull(result.Summary.Received);
        Assert.True(result.Summary.Sent.Bytes > 0);
        Assert.True(result.Summary.Received.Bytes > 0);
        Assert.True(result.Summary.EffectiveGbps > 0);

        // CPU utilization
        Assert.NotNull(result.Summary.CpuUtilization);
        Assert.True(result.Summary.CpuUtilization.LocalTotal > 0);
        Assert.True(result.Summary.CpuUtilization.RemoteTotal > 0);

        // TCP congestion algorithm
        Assert.Equal("cubic", result.Summary.TcpCongestionAlgorithm);
    }

    [Fact]
    public void Parse_SingleStreamReverse_Success()
    {
        // Arrange
        var json = LoadFixture("flowrate-iperf3-single-stream-reverse.json");

        // Act
        var result = _parser.Parse(json);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Configuration);
        Assert.Equal(Direction.Reverse, result.Configuration.Direction);
        Assert.Equal(TestProtocol.Tcp, result.Configuration.Protocol);
        Assert.Equal(1, result.Configuration.StreamCount);
    }

    [Fact]
    public void Parse_MultiStream_Success()
    {
        // Arrange
        var json = LoadFixture("flowrate-iperf3-16-stream-forward.json");

        // Act
        var result = _parser.Parse(json);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Configuration);
        Assert.Equal(16, result.Configuration.StreamCount);
        Assert.Equal(Direction.Forward, result.Configuration.Direction);

        // Verify intervals have per-stream data
        Assert.NotNull(result.Intervals);
        var firstInterval = result.Intervals[0];
        Assert.NotNull(firstInterval.PerStreamMetrics);
        Assert.Equal(16, firstInterval.PerStreamMetrics.Count);

        // Verify aggregate is higher than any individual stream
        var maxStreamThroughput = firstInterval.PerStreamMetrics.Max(s => s.BitsPerSecond);
        Assert.True(firstInterval.Aggregate.BitsPerSecond > maxStreamThroughput);
    }

    [Fact]
    public void Parse_ConnectionRefused_ReturnsError()
    {
        // Arrange
        var json = LoadFixture("flowrate-iperf3-failure-connection-refused-stdout.txt");

        // Act
        var result = _parser.Parse(json);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("unable to connect", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Connection refused", result.ErrorMessage);
        Assert.Equal("iperf 3.21", result.IperfVersion);
        Assert.Null(result.Configuration);
        Assert.Null(result.Summary);
    }

    [Fact]
    public void Parse_DnsFailure_ReturnsError()
    {
        // Arrange
        var json = LoadFixture("flowrate-iperf3-failure-dns-stdout.txt");

        // Act
        var result = _parser.Parse(json);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Name or service not known", result.ErrorMessage);
        Assert.Equal("iperf 3.21", result.IperfVersion);
    }

    [Fact]
    public void Parse_NullJson_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _parser.Parse(null!));
    }

    [Fact]
    public void Parse_EmptyJson_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _parser.Parse(""));
    }

    [Fact]
    public void Parse_InvalidJson_ThrowsJsonException()
    {
        // Act & Assert
        Assert.ThrowsAny<Exception>(() => _parser.Parse("{invalid json}"));
    }

    [Fact]
    public void ThroughputMetrics_ConvenienceProperties_CalculateCorrectly()
    {
        // Arrange
        var json = LoadFixture("flowrate-iperf3-single-stream.json");
        var result = _parser.Parse(json);

        // Act
        var summary = result.Summary!;
        var sent = summary.Sent;

        // Assert - verify convenience calculations
        Assert.Equal(sent.BitsPerSecond / 1_000_000.0, sent.Mbps);
        Assert.Equal(sent.BitsPerSecond / 1_000_000_000.0, sent.Gbps);
        Assert.Equal(sent.Bytes / 1_048_576.0, sent.MegaBytes);
        Assert.Equal(sent.Bytes / 1_073_741_824.0, sent.GigaBytes);
    }

    [Fact]
    public void BenchmarkSummary_EffectiveThroughput_UsesReceivedMetrics()
    {
        // Arrange
        var json = LoadFixture("flowrate-iperf3-single-stream.json");
        var result = _parser.Parse(json);

        // Act
        var summary = result.Summary!;

        // Assert - effective = receiver's measurement
        Assert.Equal(summary.Received.BitsPerSecond, summary.EffectiveBitsPerSecond);
        Assert.Equal(summary.Received.Gbps, summary.EffectiveGbps);
        Assert.Equal(summary.Received.Mbps, summary.EffectiveMbps);
    }

    [Fact]
    public void Parse_UdpResult_MapsJitterAndPacketLoss()
    {
        // Arrange - a minimal iperf3 UDP result: the "end" section carries a single
        // aggregate "sum" object with jitter and packet-loss instead of sum_sent/sum_received.
        const string json = """
            {
              "start": {
                "test_start": { "protocol": "UDP", "num_streams": 1, "duration": 10, "interval": 1 }
              },
              "end": {
                "sum": {
                  "start": 0, "end": 10, "seconds": 10,
                  "bytes": 1250000, "bits_per_second": 1000000,
                  "jitter_ms": 0.125, "lost_packets": 5, "packets": 1000, "lost_percent": 0.5
                }
              }
            }
            """;

        // Act
        var result = _parser.Parse(json);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Configuration);
        Assert.Equal(TestProtocol.Udp, result.Configuration!.Protocol);

        Assert.NotNull(result.Summary);
        Assert.NotNull(result.Summary!.Udp);
        Assert.Equal(0.125, result.Summary.Udp!.JitterMs);
        Assert.Equal(5, result.Summary.Udp.LostPackets);
        Assert.Equal(1000, result.Summary.Udp.Packets);
        Assert.Equal(0.5, result.Summary.Udp.LostPercent);
    }

    private static string LoadFixture(string filename)
    {
        var path = Path.Combine("Fixtures", filename);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Fixture file not found: {path}");
        }

        return File.ReadAllText(path);
    }
}
