using System.Diagnostics;
using System.Text;
using System.Text.Json;
using FlowRate.Core.Domain;
using FlowRate.Core.Iperf3;
using FlowRate.Core.Iperf3.Transport;

namespace FlowRate.Core.Services;

/// <summary>
/// Event args carrying a live interval snapshot during test execution.
/// </summary>
public sealed class IntervalProgressEventArgs : EventArgs
{
    public required IntervalSnapshot Snapshot { get; init; }
    public required double RunningAverageGbps { get; init; }
    public required double RunningAverageMbps { get; init; }
}

/// <summary>
/// Service to execute iperf3 and parse results.
/// </summary>
public sealed class Iperf3Service
{
    private readonly Iperf3Parser _parser = new();

    /// <summary>
    /// Raised on each interval as iperf3 streams live progress.
    /// Handlers are invoked from a background thread; marshal to the UI thread as needed.
    /// </summary>
    public event EventHandler<IntervalProgressEventArgs>? IntervalProgress;

    /// <summary>
    /// Run an iperf3 benchmark test.
    /// </summary>
    /// <param name="serverAddress">Server hostname or IP address</param>
    /// <param name="port">Server port (default 5201)</param>
    /// <param name="durationSeconds">Test duration in seconds</param>
    /// <param name="reverse">Use reverse mode (server sends)</param>
    /// <param name="parallelStreams">Number of parallel streams</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<BenchmarkResult> RunBenchmarkAsync(
        string serverAddress,
        int port = 5201,
        int durationSeconds = 10,
        bool reverse = false,
        int parallelStreams = 1,
        bool udp = false,
        long? targetBitrateBitsPerSecond = null,
        int? windowSizeBytes = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serverAddress))
        {
            throw new ArgumentException("Server address cannot be empty.", nameof(serverAddress));
        }

        // Build iperf3 command arguments
        var args = BuildArguments(
            serverAddress, port, durationSeconds, reverse, parallelStreams,
            udp, targetBitrateBitsPerSecond, windowSizeBytes);

        // Execute iperf3
        var (exitCode, stdout, stderr) = await ExecuteIperf3WithProgressAsync(args, cancellationToken);

        // If we got JSON output, try to parse it
        if (!string.IsNullOrWhiteSpace(stdout))
        {
            try
            {
                return _parser.Parse(stdout);
            }
            catch (Exception ex)
            {
                return new BenchmarkResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Failed to parse iperf3 output: {ex.Message}\n\nStderr: {stderr}"
                };
            }
        }

        // No stdout, return error
        return new BenchmarkResult
        {
            IsSuccess = false,
            ErrorMessage = $"iperf3 process exited with code {exitCode}\n\nStderr: {stderr}"
        };
    }

    private static string BuildArguments(
        string serverAddress,
        int port,
        int durationSeconds,
        bool reverse,
        int parallelStreams,
        bool udp,
        long? targetBitrateBitsPerSecond,
        int? windowSizeBytes)
    {
        var sb = new StringBuilder();
        sb.Append($"-c {serverAddress} ");
        sb.Append($"-p {port} ");
        sb.Append($"-t {durationSeconds} ");
        // --json-stream emits newline-delimited JSON events for live interval reporting
        // (iperf 3.17+). We reassemble a standard result blob for the existing parser.
        sb.Append("--json-stream ");

        if (udp)
        {
            sb.Append("-u ");
        }

        if (reverse)
        {
            sb.Append("-R ");
        }

        if (parallelStreams > 1)
        {
            sb.Append($"-P {parallelStreams} ");
        }

        if (targetBitrateBitsPerSecond is > 0)
        {
            // -b takes bits/sec; 0 means unlimited (TCP default), so only emit when set.
            sb.Append($"-b {targetBitrateBitsPerSecond.Value} ");
        }

        if (windowSizeBytes is > 0)
        {
            sb.Append($"-w {windowSizeBytes.Value} ");
        }

        return sb.ToString().Trim();
    }

    private async Task<(int exitCode, string stdout, string stderr)> ExecuteIperf3WithProgressAsync(
        string arguments,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "iperf3",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };

        var stderrBuilder = new StringBuilder();

        // Streaming state
        Iperf3Start? start = null;
        Iperf3End? end = null;
        string? errorMessage = null;
        var intervals = new List<Iperf3Interval>();
        var runningSumGbps = 0.0;
        var runningSumMbps = 0.0;
        var intervalCount = 0;

        process.OutputDataReceived += (_, e) =>
        {
            var line = e.Data;
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            line = line.Trim();
            if (line.Length == 0 || line[0] != '{')
            {
                return;
            }

            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;

                if (!root.TryGetProperty("event", out var eventProp))
                {
                    return;
                }

                var eventName = eventProp.GetString();
                if (!root.TryGetProperty("data", out var dataProp))
                {
                    return;
                }

                if (eventName == "error")
                {
                    errorMessage = dataProp.ValueKind == JsonValueKind.String
                        ? dataProp.GetString()
                        : dataProp.GetRawText();
                    return;
                }

                var dataJson = dataProp.GetRawText();

                switch (eventName)
                {
                    case "start":
                        start = JsonSerializer.Deserialize<Iperf3Start>(dataJson);
                        break;

                    case "interval":
                        var interval = JsonSerializer.Deserialize<Iperf3Interval>(dataJson);
                        if (interval?.Sum != null)
                        {
                            intervals.Add(interval);
                            intervalCount++;

                            var snapshot = new IntervalSnapshot
                            {
                                IntervalNumber = intervalCount,
                                Aggregate = new ThroughputMetrics
                                {
                                    StartSeconds = interval.Sum.Start,
                                    EndSeconds = interval.Sum.End,
                                    DurationSeconds = interval.Sum.Seconds,
                                    Bytes = interval.Sum.Bytes,
                                    BitsPerSecond = interval.Sum.BitsPerSecond
                                },
                                PerStreamMetrics = interval.Streams?
                                    .Select(s => new ThroughputMetrics
                                    {
                                        StartSeconds = s.Start,
                                        EndSeconds = s.End,
                                        DurationSeconds = s.Seconds,
                                        Bytes = s.Bytes,
                                        BitsPerSecond = s.BitsPerSecond
                                    })
                                    .ToList()
                            };

                            runningSumGbps += snapshot.Aggregate.Gbps;
                            runningSumMbps += snapshot.Aggregate.Mbps;

                            IntervalProgress?.Invoke(this, new IntervalProgressEventArgs
                            {
                                Snapshot = snapshot,
                                RunningAverageGbps = runningSumGbps / intervalCount,
                                RunningAverageMbps = runningSumMbps / intervalCount
                            });
                        }
                        break;

                    case "end":
                        end = JsonSerializer.Deserialize<Iperf3End>(dataJson);
                        break;
                }
            }
            catch
            {
                // Malformed or unexpected line; ignore and keep streaming.
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                stderrBuilder.AppendLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // User requested a stop: kill the iperf3 process (and its tree) so it does not
            // linger as an orphan, then rethrow so the caller can surface a cancelled state.
            TryKill(process);
            throw;
        }

        // Reassemble a standard iperf3 result blob so the existing, tested parser
        // handles the final mapping unchanged.
        var assembled = new Iperf3Result
        {
            Start = start,
            Intervals = intervals.Count > 0 ? intervals : null,
            End = end,
            Error = errorMessage
        };

        var reassembledJson = JsonSerializer.Serialize(assembled);

        return (process.ExitCode, reassembledJson, stderrBuilder.ToString());
    }

    private static void TryKill(System.Diagnostics.Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Best-effort cleanup; ignore failures.
        }
    }
}
