using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlowRate.Core.Domain;
using FlowRate.Core.Services;

namespace FlowRate.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly Iperf3Service _iperf3Service = new();

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

    [RelayCommand(CanExecute = nameof(CanRunBenchmark))]
    private async Task RunBenchmarkAsync()
    {
        IsRunning = true;
        StatusMessage = "Running benchmark...";
        ResultSummary = string.Empty;
        LastResult = null;

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
