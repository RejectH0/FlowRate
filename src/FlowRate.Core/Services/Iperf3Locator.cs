using System.Diagnostics;

namespace FlowRate.Core.Services;

/// <summary>
/// Discovers the iperf3 executable and reports its version. Used at startup to
/// verify iperf3 is installed and by the Info dialog to display detection details.
/// </summary>
public static class Iperf3Locator
{
    /// <summary>Official Windows builds of iperf3 recommended to users.</summary>
    public const string WindowsBuildsUrl = "https://github.com/ar51an/iperf3-win-builds";

    /// <summary>
    /// Resolves the full path to iperf3.exe. Search order: the application base
    /// directory (bundled copy wins), then each directory on PATH.
    /// Returns null when iperf3 cannot be found.
    /// </summary>
    public static string? FindExecutable()
    {
        var local = Path.Combine(AppContext.BaseDirectory, "iperf3.exe");
        if (File.Exists(local))
            return local;

        var pathValue = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var dir in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var candidate = Path.Combine(dir.Trim(), "iperf3.exe");
                if (File.Exists(candidate))
                    return candidate;
            }
            catch (ArgumentException)
            {
                // Malformed PATH entry; skip.
            }
        }

        return null;
    }

    /// <summary>
    /// Runs <c>iperf3 --version</c> and returns the first line (e.g. "iperf 3.19 (cJSON 1.7.15)"),
    /// or null if the executable is missing or fails to run.
    /// </summary>
    public static async Task<string?> GetVersionAsync(string? executablePath = null, CancellationToken cancellationToken = default)
    {
        executablePath ??= FindExecutable();
        if (executablePath is null)
            return null;

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process is null)
                return null;

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            var firstLine = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
            return string.IsNullOrWhiteSpace(firstLine) ? null : firstLine;
        }
        catch
        {
            return null;
        }
    }
}
