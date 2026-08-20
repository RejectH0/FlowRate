using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlowRate.Core.Domain;
using FlowRate.Core.Export;
using FlowRate.Core.History;
using FlowRate.Core.Services;
using FlowRate.Core.Diagnostics;
using FlowRate.Core.Settings;
using Microsoft.UI.Dispatching;

namespace FlowRate.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly Iperf3Service _iperf3Service = new();
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    private CancellationTokenSource? _cts;

	/// <summary>App version string shown in the header, formatted as "v0.7.1".</summary>
	[ObservableProperty]
	public partial string AppVersion { get; set; } = FormatVersion();

	/// <summary>Toggles the header version label. Kept on during development.</summary>
	[ObservableProperty]
	public partial bool IsVersionVisible { get; set; } = true;

	private static string FormatVersion()
	{
		var v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
		return v is null ? "(v0.0.0)" : $"(v{v.Major}.{v.Minor}.{v.Build})";
	}

    public MainViewModel()
    {
        _iperf3Service.IntervalProgress += OnIntervalProgress;
        LoadSettings();
        LoadHistory();
    }

    private void LoadSettings()
    {
        var settings = SettingsService.Load();
        ServerAddress = settings.ServerAddress;
        Port = settings.Port;
        DurationSeconds = settings.DurationSeconds;
        ParallelStreams = settings.ParallelStreams;
        ReverseMode = settings.ReverseMode;
        ShowAllIntervals = settings.ShowAllIntervals;
        UdpMode = settings.UdpMode;
        TargetBitrateMbps = settings.TargetBitrateMbps;
        WindowSizeKB = settings.WindowSizeKB;

        Profiles.Clear();
        foreach (var profile in settings.Profiles)
            Profiles.Add(profile);

        RecentServers.Clear();
        foreach (var server in settings.RecentServers)
            RecentServers.Add(server);
    }

    /// <summary>
    /// Builds an <see cref="AppSettings"/> snapshot from the current view-model state,
    /// preserving the persisted profile and recent-server lists.
    /// </summary>
    private AppSettings BuildSettingsSnapshot()
    {
        var settings = SettingsService.Load();
        settings.ServerAddress = ServerAddress;
        settings.Port = Port;
        settings.DurationSeconds = DurationSeconds;
        settings.ParallelStreams = ParallelStreams;
        settings.ReverseMode = ReverseMode;
        settings.ShowAllIntervals = ShowAllIntervals;
        settings.UdpMode = UdpMode;
        settings.TargetBitrateMbps = TargetBitrateMbps;
        settings.WindowSizeKB = WindowSizeKB;
        settings.Profiles = Profiles.ToList();
        settings.RecentServers = RecentServers.ToList();
        return settings;
    }

    /// <summary>
    /// Persists the current configuration as the default preferences for future sessions.
    /// </summary>
    [RelayCommand]
    private void SaveSettings()
    {
        SettingsService.Save(BuildSettingsSnapshot());
        StatusMessage = "Preferences saved";
    }

    // --- Configuration ---

    [ObservableProperty]
    public partial string ServerAddress { get; set; } = "192.168.1.100";

    [ObservableProperty]
    public partial int Port { get; set; } = 5201;

    [ObservableProperty]
    public partial int DurationSeconds { get; set; } = 10;

    [ObservableProperty]
    public partial bool ReverseMode { get; set; } = false;

    [ObservableProperty]
    public partial int ParallelStreams { get; set; } = 1;

    /// <summary>When true, uses UDP (-u) instead of TCP; enables jitter/loss reporting.</summary>
    [ObservableProperty]
    public partial bool UdpMode { get; set; } = false;

    /// <summary>Target bitrate in Mbps (0 = unlimited / iperf3 default). Applied via -b.</summary>
    [ObservableProperty]
    public partial double TargetBitrateMbps { get; set; }

    /// <summary>TCP window / socket buffer size in KB (0 = iperf3 default). Applied via -w.</summary>
    [ObservableProperty]
    public partial int WindowSizeKB { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportJsonCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportCsvCommand))]
    [NotifyCanExecuteChangedFor(nameof(RunBenchmarkCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelBenchmarkCommand))]
	[NotifyPropertyChangedFor(nameof(IsIdle))]
	[NotifyPropertyChangedFor(nameof(StatusGlyph))]
    public partial bool IsRunning { get; set; } = false;

	/// <summary>True when no benchmark is running; drives run/stop and status visuals.</summary>
	public bool IsIdle => !IsRunning;

	/// <summary>Segoe Fluent glyph for the status card: play when running, check when idle.</summary>
	public string StatusGlyph => IsRunning ? "\uE768" : "\uE73E";

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Ready";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportJsonCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportCsvCommand))]
    [NotifyCanExecuteChangedFor(nameof(CopyResultCommand))]
    public partial BenchmarkResult? LastResult { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResult))]
    [NotifyCanExecuteChangedFor(nameof(CopyResultCommand))]
    public partial string ResultSummary { get; set; } = string.Empty;

    /// <summary>True when a result summary is present; drives the results card visibility.</summary>
    public bool HasResult => !string.IsNullOrWhiteSpace(ResultSummary);

    // --- Chart, history, profiles, recent servers (v0.5.0) ---

    /// <summary>Per-interval throughput (Mbps) plotted by the throughput chart.</summary>
    public ObservableCollection<double> IntervalMbps { get; } = new();

    /// <summary>Persisted run history, newest first.</summary>
    public ObservableCollection<HistoryEntry> History { get; } = new();

    /// <summary>Saved named configuration profiles.</summary>
    public ObservableCollection<BenchmarkProfile> Profiles { get; } = new();

    /// <summary>Recently used server addresses.</summary>
    public ObservableCollection<string> RecentServers { get; } = new();

    /// <summary>
    /// Backing for the Recent Servers dropdown. Kept separate from <see cref="ServerAddress"/>
    /// so that selecting an item copies into the editable address field, while typing in the
    /// address field is never overwritten by the dropdown clearing its selection.
    /// </summary>
    [ObservableProperty]
    public partial string? SelectedRecentServer { get; set; }

    partial void OnSelectedRecentServerChanged(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            ServerAddress = value;
    }

    /// <summary>True once at least one chart point exists; reveals the chart card.</summary>
    [ObservableProperty]
    public partial bool HasChartData { get; set; }

    /// <summary>Name used when saving the current configuration as a new profile.</summary>
    [ObservableProperty]
    public partial string NewProfileName { get; set; } = string.Empty;

    /// <summary>The currently selected profile in the dropdown.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyProfileCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteProfileCommand))]
    public partial BenchmarkProfile? SelectedProfile { get; set; }

    /// <summary>The currently selected history entry; selecting one re-views it.</summary>
    [ObservableProperty]
    public partial HistoryEntry? SelectedHistory { get; set; }

    // --- Real-time interval reporting (v0.2.0) ---

    /// <summary>
    /// When true, every interval is kept in a scrolling live feed.
    /// When false, only the most recent interval and running average are shown.
    /// </summary>
    [ObservableProperty]
    public partial bool ShowAllIntervals { get; set; } = true;

    /// <summary>
    /// Rolling text feed of live intervals shown in the "Current Throughput" area.
    /// </summary>
    [ObservableProperty]
    public partial string LiveThroughputFeed { get; set; } = string.Empty;

    /// <summary>
    /// Latest interval throughput in Gbps.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentThroughputGbpsText))]
    public partial double CurrentThroughputGbps { get; set; }

    /// <summary>
    /// Latest interval throughput in Gbps, formatted to three decimal places.
    /// </summary>
    public string CurrentThroughputGbpsText => CurrentThroughputGbps.ToString("F3");

    /// <summary>
    /// Latest interval throughput in Mbps.
    /// </summary>
    [ObservableProperty]
    public partial double CurrentThroughputMbps { get; set; }

    /// <summary>
    /// Running average throughput in Gbps across all intervals so far.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AverageThroughputGbpsText))]
    public partial double AverageThroughputGbps { get; set; }

    /// <summary>
    /// Running average throughput in Gbps, formatted to three decimal places.
    /// </summary>
    public string AverageThroughputGbpsText => AverageThroughputGbps.ToString("F3");

    /// <summary>
    /// Running average throughput in Mbps across all intervals so far.
    /// </summary>
    [ObservableProperty]
    public partial double AverageThroughputMbps { get; set; }

    /// <summary>
    /// True once at least one live interval has arrived; used to reveal the live panel.
    /// </summary>
    [ObservableProperty]
    public partial bool HasLiveData { get; set; }

    /// <summary>
    /// Auto-scaling full-scale maximum (Mbps) for the speedometer gauge.
    /// Grows to the next "nice" ceiling above the observed peak; never shrinks mid-run.
    /// </summary>
    [ObservableProperty]
    public partial double GaugeMaximumMbps { get; set; } = 100;

    private double _peakMbps;

    private readonly StringBuilder _feedBuilder = new();

    [RelayCommand(CanExecute = nameof(CanRunBenchmark))]
    private async Task RunBenchmarkAsync()
    {
        IsRunning = true;
        StatusMessage = "Running benchmark...";
        ResultSummary = string.Empty;
        LastResult = null;

        // Reset live state
        _feedBuilder.Clear();
        LiveThroughputFeed = string.Empty;
        CurrentThroughputGbps = 0;
        CurrentThroughputMbps = 0;
        AverageThroughputGbps = 0;
        AverageThroughputMbps = 0;
        HasLiveData = false;
        _peakMbps = 0;
        GaugeMaximumMbps = 100;
        IntervalMbps.Clear();
        HasChartData = false;

        _cts = new CancellationTokenSource();

        Logger.Info($"Benchmark requested: server={ServerAddress}:{Port} duration={DurationSeconds}s streams={ParallelStreams} udp={UdpMode} reverse={ReverseMode} bitrate={TargetBitrateMbps}Mbps window={WindowSizeKB}KB");

        try
        {
            long? bitrateBps = TargetBitrateMbps > 0
                ? (long)(TargetBitrateMbps * 1_000_000)
                : null;
            int? windowBytes = WindowSizeKB > 0 ? WindowSizeKB * 1024 : null;

            var result = await _iperf3Service.RunBenchmarkAsync(
                ServerAddress,
                Port,
                DurationSeconds,
                ReverseMode,
                ParallelStreams,
                UdpMode,
                bitrateBps,
                windowBytes,
                _cts.Token);

            LastResult = result;

            if (result.IsSuccess)
            {
                StatusMessage = "Benchmark completed successfully";
                ResultSummary = FormatSuccessResult(result);
                RememberServer(ServerAddress);
                RecordHistory(result);
                Logger.Info("Benchmark completed successfully");
            }
            else
            {
                StatusMessage = "Benchmark failed";
                ResultSummary = $"Error: {result.ErrorMessage}";
                Logger.Warn($"Benchmark failed: {result.ErrorMessage}");
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Benchmark cancelled";
            ResultSummary = "Benchmark cancelled by user.";
            Logger.Info("Benchmark cancelled by user");
        }
        catch (Exception ex)
        {
            StatusMessage = "Error running benchmark";
            ResultSummary = $"Exception: {ex.Message}";
            Logger.Error("Unhandled exception while running benchmark", ex);
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            IsRunning = false;
        }
    }

    private bool CanCancelBenchmark() => IsRunning;

    /// <summary>Requests cancellation of the in-progress benchmark.</summary>
    [RelayCommand(CanExecute = nameof(CanCancelBenchmark))]
    private void CancelBenchmark()
    {
        StatusMessage = "Cancelling...";
        _cts?.Cancel();
    }

    private void RememberServer(string server)
    {
        var settings = BuildSettingsSnapshot();
        SettingsService.AddRecentServer(settings, server);
        SettingsService.Save(settings);

        RecentServers.Clear();
        foreach (var s in settings.RecentServers)
            RecentServers.Add(s);
    }

    private void RecordHistory(BenchmarkResult result)
    {
        var entry = RunHistoryService.Save(result);
        if (entry is not null)
            History.Insert(0, entry);
    }

    private void LoadHistory()
    {
        History.Clear();
        foreach (var entry in RunHistoryService.LoadIndex())
            History.Add(entry);
    }

    partial void OnSelectedHistoryChanged(HistoryEntry? value)
    {
        if (value is null)
            return;

        var result = RunHistoryService.LoadResult(value);
        if (result is null)
        {
            StatusMessage = "History item could not be loaded";
            return;
        }

        LastResult = result;
        ResultSummary = result.IsSuccess
            ? FormatSuccessResult(result)
            : $"Error: {result.ErrorMessage}";

        // Rebuild the chart from the stored intervals.
        IntervalMbps.Clear();
        if (result.Intervals is { } intervals)
            foreach (var snap in intervals)
                IntervalMbps.Add(snap.Aggregate.Mbps);
        HasChartData = IntervalMbps.Count > 0;

        StatusMessage = "Viewing saved run";
    }

    // --- Profiles ---

    private bool CanApplyProfile() => SelectedProfile is not null;

    /// <summary>Applies a profile automatically when it is chosen in the dropdown.</summary>
    partial void OnSelectedProfileChanged(BenchmarkProfile? value)
    {
        if (value is not null)
            ApplyProfile();
    }

    /// <summary>Applies the selected profile's values to the current configuration.</summary>
    [RelayCommand(CanExecute = nameof(CanApplyProfile))]
    private void ApplyProfile()
    {
        if (SelectedProfile is not { } p)
            return;

        ServerAddress = p.ServerAddress;
        Port = p.Port;
        DurationSeconds = p.DurationSeconds;
        ParallelStreams = p.ParallelStreams;
        ReverseMode = p.ReverseMode;
        UdpMode = p.UdpMode;
        TargetBitrateMbps = p.TargetBitrateMbps;
        WindowSizeKB = p.WindowSizeKB;
        StatusMessage = $"Applied profile '{p.Name}'";
    }

    /// <summary>Saves the current configuration as a named profile.</summary>
    [RelayCommand]
    private void SaveProfile()
    {
        // Determine the target name: an explicitly typed name creates/renames a profile,
        // otherwise fall back to the currently selected profile so "tweak and Save" just works.
        var name = NewProfileName?.Trim();
        if (string.IsNullOrEmpty(name))
            name = SelectedProfile?.Name;

        if (string.IsNullOrEmpty(name))
        {
            StatusMessage = "Select a profile or enter a name to save";
            return;
        }

        var profile = new BenchmarkProfile
        {
            Name = name,
            ServerAddress = ServerAddress,
            Port = Port,
            DurationSeconds = DurationSeconds,
            ParallelStreams = ParallelStreams,
            ReverseMode = ReverseMode,
            UdpMode = UdpMode,
            TargetBitrateMbps = TargetBitrateMbps,
            WindowSizeKB = WindowSizeKB,
        };

        var existing = Profiles.FirstOrDefault(p =>
            string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
            Profiles.Remove(existing);
        Profiles.Add(profile);

        SettingsService.Save(BuildSettingsSnapshot());
        SelectedProfile = profile;
        NewProfileName = string.Empty;
        StatusMessage = $"Saved profile '{name}'";
    }

    private bool CanDeleteProfile() => SelectedProfile is not null;

    /// <summary>Removes the selected profile.</summary>
    [RelayCommand(CanExecute = nameof(CanDeleteProfile))]
    private void DeleteProfile()
    {
        if (SelectedProfile is not { } p)
            return;

        Profiles.Remove(p);
        SelectedProfile = null;
        SettingsService.Save(BuildSettingsSnapshot());
        StatusMessage = $"Deleted profile '{p.Name}'";
    }

    // --- Copy results ---

    /// <summary>Supplied by the view: copies the given text to the system clipboard.</summary>
    public Action<string>? CopyToClipboard { get; set; }

    private bool CanCopyResult() => HasResult;

    /// <summary>Copies the formatted results text to the clipboard.</summary>
    [RelayCommand(CanExecute = nameof(CanCopyResult))]
    private void CopyResult()
    {
        CopyToClipboard?.Invoke(ResultSummary);
        StatusMessage = "Results copied to clipboard";
    }

    /// <summary>
    /// True when a successful result is available to export and no benchmark is running.
    /// </summary>
    private bool CanExportResult() => !IsRunning && LastResult is { IsSuccess: true };

    /// <summary>
    /// Supplied by the view. Given a suggested file name and format, prompts the user for a
    /// save location and returns the chosen full path, or <c>null</c> if the user cancelled.
    /// </summary>
    public Func<string, ExportFormat, Task<string?>>? SaveFilePickerAsync { get; set; }

    [RelayCommand(CanExecute = nameof(CanExportResult))]
    private Task ExportJsonAsync() => ExportResultAsync(ExportFormat.Json);

    [RelayCommand(CanExecute = nameof(CanExportResult))]
    private Task ExportCsvAsync() => ExportResultAsync(ExportFormat.Csv);

    private async Task ExportResultAsync(ExportFormat format)
    {
        if (LastResult is not { IsSuccess: true } result)
            return;

        try
        {
            var suggestedName = BenchmarkResultExporter.BuildFileName(result, format);

            string path;
            if (SaveFilePickerAsync is { } picker)
            {
                var chosen = await picker(suggestedName, format);
                if (string.IsNullOrEmpty(chosen))
                {
                    StatusMessage = "Export cancelled";
                    return;
                }

                var content = format == ExportFormat.Json
                    ? BenchmarkResultExporter.ToJson(result)
                    : BenchmarkResultExporter.ToCsv(result);
                await File.WriteAllTextAsync(chosen, content);
                path = chosen;
            }
            else
            {
                var directory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "FlowRate");
                path = BenchmarkResultExporter.Export(result, directory, format);
            }

            StatusMessage = $"Exported to {path}";
            Logger.Info($"Exported {format} result to {path}");
        }
        catch (Exception ex)
        {
            Logger.Error("Export failed", ex);
            StatusMessage = $"Export failed: {ex.Message}";
        }
    }

    private void OnIntervalProgress(object? sender, IntervalProgressEventArgs e)
    {
        // Marshal from the iperf3 background thread onto the UI thread.
        _dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                var agg = e.Snapshot.Aggregate;

                CurrentThroughputGbps = agg.Gbps;
                CurrentThroughputMbps = agg.Mbps;
                AverageThroughputGbps = e.RunningAverageGbps;
                AverageThroughputMbps = e.RunningAverageMbps;
                HasLiveData = true;

                // Feed the throughput-over-time chart.
                IntervalMbps.Add(agg.Mbps);
                HasChartData = true;

                // Auto-scale the gauge to the observed peak, snapping to a nice ceiling.
                var peakCandidate = Math.Max(agg.Mbps, e.RunningAverageMbps);
                if (peakCandidate > _peakMbps)
                {
                    _peakMbps = peakCandidate;
                    var ceiling = NiceCeiling(_peakMbps);
                    if (ceiling > GaugeMaximumMbps)
                        GaugeMaximumMbps = ceiling;
                }

                var line =
                    $"[{agg.StartSeconds,5:F1}-{agg.EndSeconds,5:F1}s]  " +
                    $"{agg.Gbps,6:F2} Gbps  ({agg.Mbps,8:F1} Mbps)   " +
                    $"avg {e.RunningAverageGbps,6:F2} Gbps";

                if (ShowAllIntervals)
                {
                    // Newest interval appears at the top of the feed.
                    _feedBuilder.Insert(0, line + Environment.NewLine);
                    LiveThroughputFeed = _feedBuilder.ToString();
                }
                else
                {
                    LiveThroughputFeed =
                        $"Interval #{e.Snapshot.IntervalNumber}\n" +
                        $"{line}";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("OnIntervalProgress UI update failed", ex);
            }
        });
    }

    private bool CanRunBenchmark() => !IsRunning;

    /// <summary>
    /// Snaps a peak throughput (Mbps) up to the next visually pleasant gauge ceiling,
    /// keeping the needle comfortably below full-scale with ~25% headroom.
    /// </summary>
    private static double NiceCeiling(double peakMbps)
    {
        var target = Math.Max(peakMbps * 1.25, 10);
        double[] baseSteps = { 1, 2, 2.5, 5 };
        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(target)));
        foreach (var mult in new[] { 1.0, 10.0 })
        {
            foreach (var step in baseSteps)
            {
                var candidate = step * magnitude * mult;
                if (candidate >= target)
                    return candidate;
            }
        }
        return magnitude * 100;
    }

    private static string FormatSuccessResult(BenchmarkResult result)
    {
        if (result.Summary == null)
            return "No summary data available.";

        var summary = result.Summary;
        var config = result.Configuration;

        var udpSection = string.Empty;
        if (summary.Udp is { } udp)
        {
            udpSection = $"""

                -------------------------------------------
                UDP Quality
                -------------------------------------------

                Jitter:       {udp.JitterMs:F3} ms
                Packet Loss:  {udp.LostPackets ?? 0}/{udp.Packets ?? 0} ({udp.LostPercent:F2}%)

                """;
        }

        return $"""
            ===========================================
            FlowRate Benchmark Results
            ===========================================

            Server:       {result.Connection?.RemoteHost ?? "Unknown"}:{result.Connection?.RemotePort ?? 0}
            Client:       {result.Connection?.LocalHost ?? "Unknown"}:{result.Connection?.LocalPort ?? 0}

            Protocol:     {config?.Protocol}
            Direction:    {config?.Direction}
            Streams:      {config?.StreamCount}
            Duration:     {config?.DurationSeconds}s

            -------------------------------------------
            Throughput
            -------------------------------------------

            Sent:         {summary.Sent.Gbps:F2} Gbps  ({summary.Sent.GigaBytes:F2} GB)
            Received:     {summary.Received.Gbps:F2} Gbps  ({summary.Received.GigaBytes:F2} GB)

            Effective:    {summary.EffectiveGbps:F2} Gbps ({summary.EffectiveMbps:F0} Mbps)
            {udpSection}
            -------------------------------------------
            CPU Utilization
            -------------------------------------------

            Local:        {summary.CpuUtilization?.LocalTotal:F1}%  (User: {summary.CpuUtilization?.LocalUser:F1}%, System: {summary.CpuUtilization?.LocalSystem:F1}%)
            Remote:       {summary.CpuUtilization?.RemoteTotal:F1}%  (User: {summary.CpuUtilization?.RemoteUser:F1}%, System: {summary.CpuUtilization?.RemoteSystem:F1}%)

            TCP Algorithm: {summary.TcpCongestionAlgorithm ?? "N/A"}

            ===========================================
            """;
    }
}
