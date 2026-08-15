using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FlowRate.Core.Domain;

namespace FlowRate.Core.Export;

/// <summary>
/// The file format a <see cref="BenchmarkResult"/> can be exported to.
/// </summary>
public enum ExportFormat
{
    Json,
    Csv,
}

/// <summary>
/// Serializes a completed <see cref="BenchmarkResult"/> to JSON or CSV and writes it to disk.
/// </summary>
public static class BenchmarkResultExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>
    /// Serializes the result to an indented JSON document.
    /// </summary>
    public static string ToJson(BenchmarkResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return JsonSerializer.Serialize(result, JsonOptions);
    }

    /// <summary>
    /// Serializes the result to a CSV document containing a summary block and one row per interval.
    /// </summary>
    public static string ToCsv(BenchmarkResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var ci = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();

        // Summary section
        sb.AppendLine("Section,Field,Value");
        sb.AppendLine($"Summary,Success,{result.IsSuccess}");
        if (!result.IsSuccess)
            sb.AppendLine($"Summary,Error,{EscapeCsv(result.ErrorMessage)}");
        sb.AppendLine($"Summary,IperfVersion,{EscapeCsv(result.IperfVersion)}");
        sb.AppendLine($"Summary,StartTime,{EscapeCsv(result.StartTime?.ToString("O", ci))}");

        if (result.Configuration is { } config)
        {
            sb.AppendLine($"Config,Protocol,{config.Protocol}");
            sb.AppendLine($"Config,Direction,{config.Direction}");
            sb.AppendLine($"Config,Streams,{config.StreamCount.ToString(ci)}");
            sb.AppendLine($"Config,DurationSeconds,{config.DurationSeconds.ToString(ci)}");
        }

        if (result.Connection is { } conn)
        {
            sb.AppendLine($"Connection,LocalHost,{EscapeCsv(conn.LocalHost)}");
            sb.AppendLine($"Connection,LocalPort,{conn.LocalPort.ToString(ci)}");
            sb.AppendLine($"Connection,RemoteHost,{EscapeCsv(conn.RemoteHost)}");
            sb.AppendLine($"Connection,RemotePort,{conn.RemotePort.ToString(ci)}");
        }

        if (result.Summary is { } summary)
        {
            sb.AppendLine($"Throughput,SentGbps,{summary.Sent.Gbps.ToString("F4", ci)}");
            sb.AppendLine($"Throughput,ReceivedGbps,{summary.Received.Gbps.ToString("F4", ci)}");
            sb.AppendLine($"Throughput,EffectiveGbps,{summary.EffectiveGbps.ToString("F4", ci)}");

            if (summary.CpuUtilization is { } cpu)
            {
                sb.AppendLine($"Cpu,LocalTotal,{cpu.LocalTotal.ToString("F2", ci)}");
                sb.AppendLine($"Cpu,RemoteTotal,{cpu.RemoteTotal.ToString("F2", ci)}");
            }

            if (!string.IsNullOrWhiteSpace(summary.TcpCongestionAlgorithm))
                sb.AppendLine($"Throughput,TcpCongestionAlgorithm,{EscapeCsv(summary.TcpCongestionAlgorithm)}");
        }

        // Interval section
        sb.AppendLine();
        sb.AppendLine("Interval,StartSeconds,EndSeconds,Mbps,Gbps,MegaBytes");
        if (result.Intervals is { } intervals)
        {
            foreach (var snapshot in intervals)
            {
                var m = snapshot.Aggregate;
                sb.AppendLine(string.Join(',',
                    snapshot.IntervalNumber.ToString(ci),
                    m.StartSeconds.ToString("F2", ci),
                    m.EndSeconds.ToString("F2", ci),
                    m.Mbps.ToString("F2", ci),
                    m.Gbps.ToString("F4", ci),
                    m.MegaBytes.ToString("F2", ci)));
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Writes the result to <paramref name="directory"/> using an auto-generated timestamped
    /// file name and returns the full path of the file written.
    /// </summary>
    public static string Export(BenchmarkResult result, string directory, ExportFormat format)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        Directory.CreateDirectory(directory);

        var content = format switch
        {
            ExportFormat.Json => ToJson(result),
            ExportFormat.Csv => ToCsv(result),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported export format."),
        };

        var path = Path.Combine(directory, BuildFileName(result, format));
        File.WriteAllText(path, content);
        return path;
    }

    /// <summary>
    /// Builds a descriptive, filesystem-safe file name such as
    /// <c>FlowRate_192-168-1-100_20260810_141500.json</c>.
    /// </summary>
    public static string BuildFileName(BenchmarkResult result, ExportFormat format)
    {
        ArgumentNullException.ThrowIfNull(result);

        var host = result.Connection?.RemoteHost ?? result.Configuration?.RemoteHost ?? "unknown";
        var safeHost = Sanitize(host);
        var timestamp = (result.StartTime ?? DateTimeOffset.Now).ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var extension = format == ExportFormat.Json ? "json" : "csv";
        return $"FlowRate_{safeHost}_{timestamp}.{extension}";
    }

    private static string Sanitize(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var ch in value)
            sb.Append(char.IsLetterOrDigit(ch) ? ch : '-');
        return sb.ToString();
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";

        return value;
    }
}
