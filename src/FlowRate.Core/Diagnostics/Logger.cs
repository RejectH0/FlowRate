using System;
using System.IO;
using System.Text;

namespace FlowRate.Core.Diagnostics;

/// <summary>
/// Minimal, dependency-free file logger for FlowRate. Writes timestamped entries to
/// <c>%LOCALAPPDATA%\FlowRate\logs\flowrate-yyyyMMdd.log</c>. Safe to call from any thread;
/// logging failures are swallowed so diagnostics never crash the app.
/// </summary>
public static class Logger
{
    private static readonly object Gate = new();

    /// <summary>Directory where log files are written.</summary>
    public static string LogDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FlowRate",
        "logs");

    public static void Info(string message) => Write("INFO", message);

    public static void Warn(string message) => Write("WARN", message);

    public static void Error(string message, Exception? ex = null)
    {
        var sb = new StringBuilder(message);
        if (ex is not null)
        {
            sb.AppendLine();
            sb.Append(ex);
        }
        Write("ERROR", sb.ToString());
    }

    private static void Write(string level, string message)
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);
            var path = Path.Combine(LogDirectory, $"flowrate-{DateTime.Now:yyyyMMdd}.log");
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}";
            lock (Gate)
            {
                File.AppendAllText(path, line);
            }
        }
        catch
        {
            // Diagnostics must never crash the app.
        }
    }
}
