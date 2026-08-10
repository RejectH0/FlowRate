using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlowRate.Core.Domain;
using FlowRate.Core.Services;
using Microsoft.UI.Dispatching;

namespace FlowRate.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly Iperf3Service _iperf3Service = new();
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    public MainViewModel()
    {
        _iperf3Service.IntervalProgress += OnIntervalProgress;
    }

    [ObservableProperty]
    private string _serverAddress = "192.168.1.100";

    [ObservableProperty]
    private int _port = 5201;

    [ObservableProperty]
    private int _durationSeconds = 10;

    [ObservableProperty]
    private bool _reverseMode = false;

    [ObservableProperty]
    private int _parallelStreams = 1;

    [ObservableProperty]
    private bool _isRunning = false;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private BenchmarkResult? _lastResult;

    [ObservableProperty]
    private string _resultSummary = string.Empty;

    // --- Real-time interval reporting (v0.2.0) ---

    /// <summary>
    /// When true, every interval is kept in a scrolling live feed.
    /// When false, only the most recent interval and running average are shown.
    /// </summary>
    [ObservableProperty]
    private bool _showAllIntervals = true;

    /// <summary>
    /// Rolling text feed of live intervals shown in the "Current Throughput" area.
    /// </summary>
    [ObservableProperty]
    private string _liveThroughputFeed = string.Empty;

    /// <summary>
    /// Latest interval throughput in Gbps.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentThroughputGbpsText))]
    private double _currentThroughputGbps;

    /// <summary>
    /// Latest interval throughput in Gbps, formatted to three decimal places.
    /// </summary>
    public string CurrentThroughputGbpsText => CurrentThroughputGbps.ToString("F3");

    /// <summary>
    /// Latest interval throughput in Mbps.
    /// </summary>
    [ObservableProperty]
    private double _currentThroughputMbps;

    /// <summary>
    /// Running average throughput in Gbps across all intervals so far.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AverageThroughputGbpsText))]
    private double _averageThroughputGbps;

    /// <summary>
    /// Running average throughput in Gbps, formatted to three decimal places.
    /// </summary>
    public string AverageThroughputGbpsText => AverageThroughputGbps.ToString("F3");

    /// <summary>
    /// Running average throughput in Mbps across all intervals so far.
    /// </summary>
    [ObservableProperty]
    private double _averageThroughputMbps;

    /// <summary>
    /// True once at least one live interval has arrived; used to reveal the live panel.
    /// </summary>
    [ObservableProperty]
    private bool _hasLiveData;

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

    private void OnIntervalProgress(object? sender, IntervalProgressEventArgs e)
    {
        // Marshal from the iperf3 background thread onto the UI thread.
        _dispatcherQueue.TryEnqueue(() =>
        {
            var agg = e.Snapshot.Aggregate;

            CurrentThroughputGbps = agg.Gbps;
            CurrentThroughputMbps = agg.Mbps;
            AverageThroughputGbps = e.RunningAverageGbps;
            AverageThroughputMbps = e.RunningAverageMbps;
            HasLiveData = true;

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
        });
    }

    private bool CanRunBenchmark() => !IsRunning;

    private static string FormatSuccessResult(BenchmarkResult result)
    {
        if (result.Summary == null)
            return "No summary data available.";

        var summary = result.Summary;
        var config = result.Configuration;

        return $"""
            ═══════════════════════════════════════════════
            FlowRate Benchmark Results
            ═══════════════════════════════════════════════

            Server:       {result.Connection?.RemoteHost ?? "Unknown"}:{result.Connection?.RemotePort ?? 0}
            Client:       {result.Connection?.LocalHost ?? "Unknown"}:{result.Connection?.LocalPort ?? 0}

            Protocol:     {config?.Protocol}
            Direction:    {config?.Direction}
            Streams:      {config?.StreamCount}
            Duration:     {config?.DurationSeconds}s

            ───────────────────────────────────────────────
            Throughput
            ───────────────────────────────────────────────

            Sent:         {summary.Sent.Gbps:F2} Gbps  ({summary.Sent.GigaBytes:F2} GB)
            Received:     {summary.Received.Gbps:F2} Gbps  ({summary.Received.GigaBytes:F2} GB)

            Effective:    {summary.EffectiveGbps:F2} Gbps ({summary.EffectiveMbps:F0} Mbps)

            ───────────────────────────────────────────────
            CPU Utilization
            ───────────────────────────────────────────────

            Local:        {summary.CpuUtilization?.LocalTotal:F1}%  (User: {summary.CpuUtilization?.LocalUser:F1}%, System: {summary.CpuUtilization?.LocalSystem:F1}%)
            Remote:       {summary.CpuUtilization?.RemoteTotal:F1}%  (User: {summary.CpuUtilization?.RemoteUser:F1}%, System: {summary.CpuUtilization?.RemoteSystem:F1}%)

            TCP Algorithm: {summary.TcpCongestionAlgorithm ?? "N/A"}

            ═══════════════════════════════════════════════
            """;
    }
}
