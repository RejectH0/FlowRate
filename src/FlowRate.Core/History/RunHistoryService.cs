using System.Text.Json;
using System.Text.Json.Serialization;
using FlowRate.Core.Diagnostics;
using FlowRate.Core.Domain;

namespace FlowRate.Core.History;

/// <summary>
/// Persists completed benchmark runs so they can be reviewed and re-exported across sessions.
/// Each run's full <see cref="BenchmarkResult"/> is stored as an individual JSON file in
/// <c>%LOCALAPPDATA%\FlowRate\history\</c>, with a capped <c>index.json</c> describing them.
/// Failures are logged and swallowed so history never blocks a benchmark.
/// </summary>
public static class RunHistoryService
{
    /// <summary>Maximum number of runs retained; oldest are pruned beyond this.</summary>
    public const int MaxEntries = 100;

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Directory holding history JSON files and the index.</summary>
    public static string HistoryDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FlowRate",
        "history");

    private static string IndexPath => Path.Combine(HistoryDirectory, "index.json");

    /// <summary>
    /// Loads the run index, newest first. Returns an empty list on any failure.
    /// </summary>
    public static IReadOnlyList<HistoryEntry> LoadIndex()
    {
        try
        {
            if (!File.Exists(IndexPath))
                return Array.Empty<HistoryEntry>();

            var json = File.ReadAllText(IndexPath);
            var entries = JsonSerializer.Deserialize<List<HistoryEntry>>(json, Options)
                          ?? new List<HistoryEntry>();
            return entries.OrderByDescending(e => e.Timestamp).ToList();
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to load run history index.", ex);
            return Array.Empty<HistoryEntry>();
        }
    }

    /// <summary>
    /// Persists a successful result to history and returns the created index entry, or
    /// <c>null</c> if it was not saved.
    /// </summary>
    public static HistoryEntry? Save(BenchmarkResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (!result.IsSuccess)
            return null;

        try
        {
            Directory.CreateDirectory(HistoryDirectory);

            var timestamp = result.StartTime ?? DateTimeOffset.Now;
            var fileName = $"run_{timestamp:yyyyMMdd_HHmmss_fff}.json";
            var fullPath = Path.Combine(HistoryDirectory, fileName);
            File.WriteAllText(fullPath, JsonSerializer.Serialize(result, Options));

            var entry = new HistoryEntry
            {
                FileName = fileName,
                Timestamp = timestamp,
                Server = result.Connection?.RemoteHost ?? result.Configuration?.RemoteHost,
                Port = result.Configuration?.RemotePort,
                Protocol = result.Configuration?.Protocol.ToString(),
                EffectiveMbps = result.Summary?.EffectiveMbps ?? 0,
                IsSuccess = result.IsSuccess,
            };

            var entries = LoadIndex().ToList();
            entries.Insert(0, entry);
            Prune(entries);
            File.WriteAllText(IndexPath, JsonSerializer.Serialize(entries, Options));

            return entry;
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to save run to history.", ex);
            return null;
        }
    }

    /// <summary>
    /// Loads the full stored result for a history entry, or <c>null</c> if unavailable.
    /// </summary>
    public static BenchmarkResult? LoadResult(HistoryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        try
        {
            var fullPath = Path.Combine(HistoryDirectory, entry.FileName);
            if (!File.Exists(fullPath))
                return null;

            var json = File.ReadAllText(fullPath);
            return JsonSerializer.Deserialize<BenchmarkResult>(json, Options);
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to load history result.", ex);
            return null;
        }
    }

    /// <summary>Removes all stored runs and the index.</summary>
    public static void Clear()
    {
        try
        {
            if (Directory.Exists(HistoryDirectory))
                Directory.Delete(HistoryDirectory, recursive: true);
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to clear run history.", ex);
        }
    }

    private static void Prune(List<HistoryEntry> entries)
    {
        while (entries.Count > MaxEntries)
        {
            var oldest = entries[^1];
            entries.RemoveAt(entries.Count - 1);
            try
            {
                var path = Path.Combine(HistoryDirectory, oldest.FileName);
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to prune old history file.", ex);
            }
        }
    }
}
