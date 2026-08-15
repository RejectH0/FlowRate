using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlowRate.Core.Domain;
using FlowRate.Core.Export;
using FlowRate.Core.Services;
using FlowRate.Core.Diagnostics;
using FlowRate.Core.Settings;
using Microsoft.UI.Dispatching;

namespace FlowRate.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly Iperf3Service _iperf3Service = new();
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    public MainViewModel()
    {
        _iperf3Service.IntervalProgress += OnIntervalProgress;
        LoadSettings();
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
    }

    /// <summary>
    /// Persists the current configuration as the default preferences for future sessions.
    /// </summary>
    [RelayCommand]
    private void SaveSettings()
    {
        SettingsService.Save(new AppSettings
        {
            ServerAddress = ServerAddress,
            Port = Port,
            DurationSeconds = DurationSeconds,
            ParallelStreams = ParallelStreams,
            ReverseMode = ReverseMode,
            ShowAllIntervals = ShowAllIntervals,
        });
        StatusMessage = "Preferences saved";
    }

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

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportJsonCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportCsvCommand))]
    public partial bool IsRunning { get; set; } = false;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Ready";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportJsonCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportCsvCommand))]
    public partial BenchmarkResult? LastResult { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResult))]
    public partial string ResultSummary { get; set; } = string.Empty;

    /// <summary>True when a result summary is present; drives the results card visibility.</summary>
    public bool HasResult => !string.IsNullOrWhiteSpace(ResultSummary);

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

        try
        {
            var result = await _iperf3Service.RunBenchmarkAsync(
                ServerAddress,
                Port,
                DurationSeconds,
                ReverseMode,
                ParallelStreams);

            LastResult = result;

            if (result.IsSuccess)
            {
                StatusMessage = "Benchmark completed successfully";
                ResultSummary = FormatSuccessResult(result);
            }
            else
            {
                StatusMessage = "Benchmark failed";
                ResultSummary = $"Error: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = "Error running benchmark";
            ResultSummary = $"Exception: {ex.Message}";
        }
        finally
        {
            IsRunning = false;
        }
    }

    /// <summary>
    /// True when a successful result is available to export and no benchmark is running.
    /// </summary>
    private bool CanExportResult() => !IsRunning && LastResult is { IsSuccess: true };

    [RelayCommand(CanExecute = nameof(CanExportResult))]
    private void ExportJson() => ExportResult(ExportFormat.Json);

    [RelayCommand(CanExecute = nameof(CanExportResult))]
    private void ExportCsv() => ExportResult(ExportFormat.Csv);

    private void ExportResult(ExportFormat format)
    {
        if (LastResult is not { IsSuccess: true } result)
            return;

        try
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "FlowRate");

            var path = BenchmarkResultExporter.Export(result, directory, format);
            StatusMessage = $"Exported to {Path.GetFileName(path)}";
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
