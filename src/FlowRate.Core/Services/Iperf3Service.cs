using System.Diagnostics;
using System.Text;
using FlowRate.Core.Domain;
using FlowRate.Core.Iperf3;

namespace FlowRate.Core.Services;

/// <summary>
/// Service to execute iperf3 and parse results.
/// </summary>
public sealed class Iperf3Service
{
    private readonly Iperf3Parser _parser = new();

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
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serverAddress))
        {
            throw new ArgumentException("Server address cannot be empty.", nameof(serverAddress));
        }

        // Build iperf3 command arguments
        var args = BuildArguments(serverAddress, port, durationSeconds, reverse, parallelStreams);

        // Execute iperf3
        var (exitCode, stdout, stderr) = await ExecuteIperf3Async(args, cancellationToken);

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
        int parallelStreams)
    {
        var sb = new StringBuilder();
        sb.Append($"-c {serverAddress} ");
        sb.Append($"-p {port} ");
        sb.Append($"-t {durationSeconds} ");
        sb.Append("-J "); // JSON output

        if (reverse)
        {
            sb.Append("-R ");
        }

        if (parallelStreams > 1)
        {
            sb.Append($"-P {parallelStreams} ");
        }

        return sb.ToString().Trim();
    }

    private static async Task<(int exitCode, string stdout, string stderr)> ExecuteIperf3Async(
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

        var stdoutBuilder = new StringBuilder();
        var stderrBuilder = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                stdoutBuilder.AppendLine(e.Data);
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

        await process.WaitForExitAsync(cancellationToken);

        return (process.ExitCode, stdoutBuilder.ToString(), stderrBuilder.ToString());
    }
}
