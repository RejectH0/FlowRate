using System.Text.Json;
using FlowRate.Core.Domain;
using FlowRate.Core.Iperf3.Transport;

namespace FlowRate.Core.Iperf3;

/// <summary>
/// Parses iperf3 JSON output and maps it to FlowRate domain models.
/// </summary>
public sealed class Iperf3Parser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    /// <summary>
    /// Parse iperf3 JSON output from a string.
    /// </summary>
    public BenchmarkResult Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("JSON input cannot be null or empty.", nameof(json));
        }

        var transport = JsonSerializer.Deserialize<Iperf3Result>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize iperf3 JSON.");

        return MapToDomain(transport);
    }

    /// <summary>
    /// Map transport model to domain model.
    /// </summary>
    private static BenchmarkResult MapToDomain(Iperf3Result transport)
    {
        // Check for error condition
        if (!transport.IsSuccess)
        {
            return new BenchmarkResult
            {
                IsSuccess = false,
                ErrorMessage = transport.Error,
                IperfVersion = transport.Start?.Version
            };
        }

        // Successful result
        var start = transport.Start!;
        var testStart = start.TestStart!;

        return new BenchmarkResult
        {
            IsSuccess = true,
            IperfVersion = start.Version,
            StartTime = MapTimestamp(start.Timestamp),
            Configuration = MapConfiguration(testStart, start.ConnectingTo),
            Connection = MapConnection(start.Connected?.FirstOrDefault()),
            Intervals = MapIntervals(transport.Intervals),
            Summary = MapSummary(transport.End)
        };
    }

    private static DateTimeOffset? MapTimestamp(Iperf3Timestamp? timestamp)
    {
        if (timestamp == null)
            return null;

        return DateTimeOffset.FromUnixTimeSeconds(timestamp.Timesecs);
    }

    private static BenchmarkConfiguration MapConfiguration(
        Iperf3TestStart testStart,
        Iperf3Endpoint? endpoint)
    {
        return new BenchmarkConfiguration
        {
            Protocol = ParseProtocol(testStart.Protocol),
            Direction = MapDirection(testStart.Reverse, testStart.Bidir),
            StreamCount = testStart.NumStreams,
            DurationSeconds = testStart.Duration,
            IntervalSeconds = testStart.Interval,
            BlockSize = testStart.Blksize,
            OmitSeconds = testStart.Omit,
            TargetBitrate = testStart.TargetBitrate > 0 ? testStart.TargetBitrate : null,
            TypeOfService = testStart.Tos > 0 ? testStart.Tos : null,
            RemoteHost = endpoint?.Host,
            RemotePort = endpoint?.Port
        };
    }

    private static TestProtocol ParseProtocol(string? protocol)
    {
        return protocol?.ToUpperInvariant() switch
        {
            "TCP" => TestProtocol.Tcp,
            "UDP" => TestProtocol.Udp,
            "SCTP" => TestProtocol.Sctp,
            _ => TestProtocol.Tcp
        };
    }

    private static Direction MapDirection(int reverse, int bidir)
    {
        if (bidir > 0)
            return Direction.Bidirectional;

        return reverse > 0 ? Direction.Reverse : Direction.Forward;
    }

    private static ConnectionInfo? MapConnection(Iperf3Connection? connection)
    {
        if (connection == null || string.IsNullOrEmpty(connection.LocalHost))
            return null;

        return new ConnectionInfo
        {
            LocalHost = connection.LocalHost,
            LocalPort = connection.LocalPort,
            RemoteHost = connection.RemoteHost ?? string.Empty,
            RemotePort = connection.RemotePort
        };
    }

    private static IReadOnlyList<IntervalSnapshot>? MapIntervals(
        List<Iperf3Interval>? intervals)
    {
        if (intervals == null || intervals.Count == 0)
            return null;

        var result = new List<IntervalSnapshot>();
        for (int i = 0; i < intervals.Count; i++)
        {
            var interval = intervals[i];
            if (interval.Sum == null)
                continue;

            var snapshot = new IntervalSnapshot
            {
                IntervalNumber = i + 1,
                Aggregate = MapThroughput(interval.Sum),
                PerStreamMetrics = interval.Streams?.Select(MapThroughput).ToList()
            };

            result.Add(snapshot);
        }

        return result;
    }

    private static ThroughputMetrics MapThroughput(Iperf3Stream stream)
    {
        return new ThroughputMetrics
        {
            StartSeconds = stream.Start,
            EndSeconds = stream.End,
            DurationSeconds = stream.Seconds,
            Bytes = stream.Bytes,
            BitsPerSecond = stream.BitsPerSecond,
            JitterMs = stream.JitterMs,
            LostPackets = stream.LostPackets,
            Packets = stream.Packets,
            LostPercent = stream.LostPercent
        };
    }

    private static BenchmarkSummary? MapSummary(Iperf3End? end)
    {
        if (end == null)
            return null;

        // UDP tests report a single aggregate "sum" (with jitter/loss) rather than
        // the TCP sum_sent/sum_received pair. Fall back to it for both directions.
        var sent = end.SumSent ?? end.Sum;
        var received = end.SumReceived ?? end.Sum;
        if (sent == null || received == null)
            return null;

        return new BenchmarkSummary
        {
            Sent = MapThroughput(sent),
            Received = MapThroughput(received),
            CpuUtilization = MapCpuUtilization(end.CpuUtilizationPercent),
            TcpCongestionAlgorithm = end.ReceiverTcpCongestion,
            Udp = end.Sum is { } udpSum ? MapThroughput(udpSum) : null
        };
    }

    private static CpuUtilization? MapCpuUtilization(Iperf3CpuUtilization? cpu)
    {
        if (cpu == null)
            return null;

        return new CpuUtilization
        {
            LocalTotal = cpu.HostTotal,
            LocalUser = cpu.HostUser,
            LocalSystem = cpu.HostSystem,
            RemoteTotal = cpu.RemoteTotal,
            RemoteUser = cpu.RemoteUser,
            RemoteSystem = cpu.RemoteSystem
        };
    }
}
