using System;
using System.IO;
using System.Text.Json;
using FlowRate.Core.Diagnostics;

namespace FlowRate.Core.Settings;

/// <summary>
/// Loads and saves <see cref="AppSettings"/> as JSON in
/// <c>%LOCALAPPDATA%\FlowRate\settings.json</c>. Failures are logged and swallowed so a
/// corrupt or unreadable settings file never prevents the app from starting.
/// </summary>
public static class SettingsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    /// <summary>Full path to the persisted settings file.</summary>
    public static string SettingsPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FlowRate",
        "settings.json");

    /// <summary>
    /// Loads persisted settings, returning defaults when the file is missing or invalid.
    /// </summary>
    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return new AppSettings();

            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to load settings; using defaults.", ex);
            return new AppSettings();
        }
    }

    /// <summary>
    /// Persists the supplied settings to disk, creating the directory if needed.
    /// </summary>
    public static void Save(AppSettings settings)
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(settings, SerializerOptions);
            File.WriteAllText(SettingsPath, json);
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to save settings.", ex);
        }
    }

    /// <summary>Maximum number of recent server addresses retained.</summary>
    public const int MaxRecentServers = 8;

    /// <summary>
    /// Records a server address at the top of the recent list (de-duplicated, capped),
    /// mutating the supplied settings in place.
    /// </summary>
    public static void AddRecentServer(AppSettings settings, string server)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (string.IsNullOrWhiteSpace(server))
            return;

        settings.RecentServers.RemoveAll(s =>
            string.Equals(s, server, StringComparison.OrdinalIgnoreCase));
        settings.RecentServers.Insert(0, server);

        while (settings.RecentServers.Count > MaxRecentServers)
            settings.RecentServers.RemoveAt(settings.RecentServers.Count - 1);
    }
}
