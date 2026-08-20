using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace FlowRate.Core.Diagnostics;

/// <summary>
/// Builds a diagnostics bundle (zip) that users can attach to GitHub Issues.
/// Contains recent FlowRate log files, the app settings file, and an environment
/// summary (app version, OS, runtime, architecture). No data outside FlowRate's
/// own %LOCALAPPDATA%\FlowRate folder is collected.
/// </summary>
public static class DiagnosticsService
{
    /// <summary>Maximum number of most-recent log files included in the bundle.</summary>
    private const int MaxLogFiles = 7;

    /// <summary>
    /// Create a diagnostics zip at <paramref name="destinationPath"/>.
    /// Overwrites any existing file at that path.
    /// </summary>
    /// <param name="appVersion">The application version string to record in the environment summary.</param>
    /// <param name="destinationPath">Full path of the zip file to create.</param>
    /// <param name="settingsPath">Path to settings.json (included if it exists).</param>
    /// <returns>The path of the created zip.</returns>
    public static string CreateBundle(string appVersion, string destinationPath, string? settingsPath = null)
    {
        Logger.Info($"Creating diagnostics bundle at {destinationPath}");

        if (File.Exists(destinationPath))
        {
            File.Delete(destinationPath);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

        using var archive = ZipFile.Open(destinationPath, ZipArchiveMode.Create);

        // Environment summary
        var summary = BuildEnvironmentSummary(appVersion);
        var summaryEntry = archive.CreateEntry("environment.txt");
        using (var writer = new StreamWriter(summaryEntry.Open(), Encoding.UTF8))
        {
            writer.Write(summary);
        }

        // Recent log files
        if (Directory.Exists(Logger.LogDirectory))
        {
            var logs = Directory.GetFiles(Logger.LogDirectory, "flowrate-*.log")
                .OrderByDescending(f => f, StringComparer.OrdinalIgnoreCase)
                .Take(MaxLogFiles);

            foreach (var log in logs)
            {
                AddFileSafe(archive, log, $"logs/{Path.GetFileName(log)}");
            }
        }

        // Settings snapshot
        if (!string.IsNullOrEmpty(settingsPath) && File.Exists(settingsPath))
        {
            AddFileSafe(archive, settingsPath, "settings.json");
        }

        Logger.Info("Diagnostics bundle created successfully");
        return destinationPath;
    }

    /// <summary>Suggested file name for a new diagnostics bundle.</summary>
    public static string SuggestFileName() =>
        $"flowrate-diagnostics-{DateTime.Now:yyyyMMdd-HHmmss}.zip";

    private static void AddFileSafe(ZipArchive archive, string sourcePath, string entryName)
    {
        try
        {
            // Copy via stream so in-use (open for append) log files can still be read.
            var entry = archive.CreateEntry(entryName);
            using var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var target = entry.Open();
            source.CopyTo(target);
        }
        catch (Exception ex)
        {
            Logger.Warn($"Diagnostics bundle: could not include '{sourcePath}': {ex.Message}");
        }
    }

    private static string BuildEnvironmentSummary(string appVersion)
    {
        var sb = new StringBuilder();
        sb.AppendLine("FlowRate Diagnostics Bundle");
        sb.AppendLine("===========================");
        sb.AppendLine($"Generated:      {DateTime.Now:yyyy-MM-dd HH:mm:ss} (local)");
        sb.AppendLine($"App Version:    {appVersion}");
        sb.AppendLine($"OS:             {RuntimeInformation.OSDescription}");
        sb.AppendLine($"OS Arch:        {RuntimeInformation.OSArchitecture}");
        sb.AppendLine($"Process Arch:   {RuntimeInformation.ProcessArchitecture}");
        sb.AppendLine($"Runtime:        {RuntimeInformation.FrameworkDescription}");
        sb.AppendLine($"Machine:        {Environment.MachineName}");
        sb.AppendLine($"64-bit OS:      {Environment.Is64BitOperatingSystem}");
        sb.AppendLine($"Processor Count:{Environment.ProcessorCount}");
        sb.AppendLine($"Log Directory:  {Logger.LogDirectory}");
        return sb.ToString();
    }
}
