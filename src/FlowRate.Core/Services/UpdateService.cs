using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace FlowRate.Core.Services;

/// <summary>Result of a GitHub release update check.</summary>
public sealed record UpdateCheckResult(
    bool Succeeded,
    string? LatestVersion,
    string? ReleaseUrl,
    bool IsUpdateAvailable,
    string? Message);

/// <summary>
/// Checks GitHub for newer releases of FlowRate and of the iperf3 Windows builds
/// using the public releases/latest API (unauthenticated; 60 requests/hour limit,
/// which is ample for manual, user-initiated checks).
/// </summary>
public sealed class UpdateService
{
    /// <summary>GitHub repository hosting FlowRate releases.</summary>
    public const string FlowRateRepo = "RejectH0/FlowRate";

    /// <summary>GitHub repository hosting the recommended iperf3 Windows builds.</summary>
    public const string Iperf3Repo = "ar51an/iperf3-win-builds";

    private static readonly HttpClient Http = CreateClient();

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("FlowRate", "1.0"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return client;
    }

    /// <summary>Checks whether a newer FlowRate release exists on GitHub.</summary>
    public Task<UpdateCheckResult> CheckFlowRateAsync(Version currentVersion, CancellationToken cancellationToken = default)
        => CheckRepoAsync(FlowRateRepo, currentVersion, cancellationToken);

    /// <summary>
    /// Checks the latest iperf3 Windows build release. The installed iperf3 version string
    /// (e.g. "iperf 3.19 (cJSON 1.7.15)") is parsed for comparison when possible.
    /// </summary>
    public Task<UpdateCheckResult> CheckIperf3Async(string? installedVersionText, CancellationToken cancellationToken = default)
        => CheckRepoAsync(Iperf3Repo, ParseVersion(installedVersionText), cancellationToken);

    private static async Task<UpdateCheckResult> CheckRepoAsync(string repo, Version? current, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await Http.GetAsync($"https://api.github.com/repos/{repo}/releases/latest", cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return new UpdateCheckResult(true, null, $"https://github.com/{repo}/releases", false, "No releases published yet.");

            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
            var tag = doc.RootElement.GetProperty("tag_name").GetString();
            var url = doc.RootElement.TryGetProperty("html_url", out var u) ? u.GetString() : $"https://github.com/{repo}/releases";

            var latest = ParseVersion(tag);
            var updateAvailable = current is not null && latest is not null && latest > current;

            var message = updateAvailable
                ? $"Update available: {tag}"
                : latest is null || current is null
                    ? $"Latest release: {tag}"
                    : "You are up to date.";

            return new UpdateCheckResult(true, tag, url, updateAvailable, message);
        }
        catch (Exception ex)
        {
            return new UpdateCheckResult(false, null, $"https://github.com/{repo}/releases", false, $"Update check failed: {ex.Message}");
        }
    }

    /// <summary>Extracts a Version from free-form text such as "v0.7.0", "iperf 3.19 (cJSON 1.7.15)", or "3.19-win64".</summary>
    public static Version? ParseVersion(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var match = System.Text.RegularExpressions.Regex.Match(text, @"(\d+)\.(\d+)(?:\.(\d+))?");
        if (!match.Success)
            return null;

        var major = int.Parse(match.Groups[1].Value);
        var minor = int.Parse(match.Groups[2].Value);
        var build = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;
        return new Version(major, minor, build);
    }
}
