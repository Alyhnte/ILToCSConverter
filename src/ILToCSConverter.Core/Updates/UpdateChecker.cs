using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ILToCSConverter.Core.Updates;

public sealed record UpdateCheckResult(
    bool Success,
    bool UpdateAvailable,
    string CurrentVersion,
    string? LatestVersion,
    string? ReleaseUrl,
    string? ReleaseName,
    string? Error);

public static class VersionComparer
{
    public static bool IsNewer(string remote, string current)
    {
        if (!TryParse(remote, out var remoteVersion) || !TryParse(current, out var currentVersion))
            return string.Compare(Normalize(remote), Normalize(current), StringComparison.OrdinalIgnoreCase) > 0;

        return remoteVersion > currentVersion;
    }

    public static string Normalize(string value)
    {
        value = value.Trim();
        if (value.StartsWith('v') || value.StartsWith('V'))
            value = value[1..];
        return value.Trim();
    }

    private static bool TryParse(string value, out Version version) =>
        Version.TryParse(Normalize(value), out version!);
}

public static class UpdateChecker
{
    private static readonly HttpClient Http = CreateClient();

    public static async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await Http.GetAsync(ProductInfo.GitHubLatestReleaseApi, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new UpdateCheckResult(
                    Success: true,
                    UpdateAvailable: false,
                    CurrentVersion: ProductInfo.Version,
                    LatestVersion: null,
                    ReleaseUrl: ProductInfo.GitHubReleasesUrl,
                    ReleaseName: null,
                    Error: null);
            }

            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = doc.RootElement;
            string tag = root.TryGetProperty("tag_name", out var tagEl) ? tagEl.GetString() ?? "" : "";
            string url = root.TryGetProperty("html_url", out var urlEl) ? urlEl.GetString() ?? ProductInfo.GitHubReleasesUrl : ProductInfo.GitHubReleasesUrl;
            string? name = root.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : tag;

            return new UpdateCheckResult(
                Success: true,
                UpdateAvailable: !string.IsNullOrWhiteSpace(tag) && VersionComparer.IsNewer(tag, ProductInfo.Version),
                CurrentVersion: ProductInfo.Version,
                LatestVersion: string.IsNullOrWhiteSpace(tag) ? null : VersionComparer.Normalize(tag),
                ReleaseUrl: url,
                ReleaseName: string.IsNullOrWhiteSpace(name) ? tag : name,
                Error: null);
        }
        catch (Exception ex)
        {
            return new UpdateCheckResult(
                Success: false,
                UpdateAvailable: false,
                CurrentVersion: ProductInfo.Version,
                LatestVersion: null,
                ReleaseUrl: ProductInfo.GitHubReleasesUrl,
                ReleaseName: null,
                Error: ex.Message);
        }
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"{ProductInfo.CliName}/{ProductInfo.Version}");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return client;
    }
}
