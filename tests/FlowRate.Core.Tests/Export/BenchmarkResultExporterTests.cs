using System.Text.Json;
using FlowRate.Core.Domain;
using FlowRate.Core.Export;
using FlowRate.Core.Iperf3;

namespace FlowRate.Core.Tests.Export;

/// <summary>
/// Tests for <see cref="BenchmarkResultExporter"/> using a real parsed fixture result.
/// </summary>
public class BenchmarkResultExporterTests
{
    private readonly Iperf3Parser _parser = new();

    [Fact]
    public void ToJson_SuccessfulResult_ProducesValidJson()
    {
        var result = ParseFixture("flowrate-iperf3-single-stream.json");

        var json = BenchmarkResultExporter.ToJson(result);

        Assert.False(string.IsNullOrWhiteSpace(json));
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("IsSuccess").GetBoolean());
        Assert.True(doc.RootElement.TryGetProperty("Summary", out _));
    }

    [Fact]
    public void ToCsv_SuccessfulResult_ContainsSummaryAndIntervalRows()
    {
        var result = ParseFixture("flowrate-iperf3-single-stream.json");

        var csv = BenchmarkResultExporter.ToCsv(result);

        Assert.Contains("Section,Field,Value", csv);
        Assert.Contains("Summary,Success,True", csv);
        Assert.Contains("Interval,StartSeconds,EndSeconds,Mbps,Gbps,MegaBytes", csv);
        // At least one interval data row beyond the header.
        Assert.NotNull(result.Intervals);
        Assert.True(result.Intervals!.Count > 0);
    }

    [Fact]
    public void BuildFileName_UsesRemoteHostAndExtension()
    {
        var result = ParseFixture("flowrate-iperf3-single-stream.json");

        var jsonName = BenchmarkResultExporter.BuildFileName(result, ExportFormat.Json);
        var csvName = BenchmarkResultExporter.BuildFileName(result, ExportFormat.Csv);

        Assert.StartsWith("FlowRate_", jsonName);
        Assert.EndsWith(".json", jsonName);
        Assert.EndsWith(".csv", csvName);
        Assert.DoesNotContain(":", jsonName);
        Assert.DoesNotContain("/", jsonName);
    }

    [Fact]
    public void Export_WritesFileToDisk_AndReturnsPath()
    {
        var result = ParseFixture("flowrate-iperf3-single-stream.json");
        var dir = Path.Combine(Path.GetTempPath(), "FlowRateExportTests", Guid.NewGuid().ToString("N"));

        try
        {
            var path = BenchmarkResultExporter.Export(result, dir, ExportFormat.Csv);

            Assert.True(File.Exists(path));
            Assert.Equal(dir, Path.GetDirectoryName(path));
            var content = File.ReadAllText(path);
            Assert.Contains("Summary,Success,True", content);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void ToJson_NullResult_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => BenchmarkResultExporter.ToJson(null!));
    }

    private BenchmarkResult ParseFixture(string filename)
    {
        var path = Path.Combine("Fixtures", filename);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Fixture file not found: {path}");

        return _parser.Parse(File.ReadAllText(path));
    }
}
