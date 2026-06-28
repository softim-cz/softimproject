using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SoftimProject.Application.Features.Migration.EasyProject.Models;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Infrastructure.Services.EasyProject;

public sealed class EasyProjectApiClient(HttpClient httpClient, ILogger<EasyProjectApiClient> logger) : IEasyProjectApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<(bool Success, string? Error)> TestConnectionAsync(string baseUrl, string apiKey, CancellationToken ct)
    {
        try
        {
            var url = BuildUrl(baseUrl, "users/current", apiKey);
            var response = await httpClient.GetAsync(url, ct);
            if (response.IsSuccessStatusCode)
                return (true, null);
            return (false, $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "EasyProject connection test failed for {BaseUrl}", baseUrl);
            return (false, ex.Message);
        }
    }

    public async Task<List<EpProject>> GetProjectsAsync(string baseUrl, string apiKey, CancellationToken ct)
    {
        return await GetAllPaginatedAsync<EpProject>(baseUrl, "projects", apiKey, "projects", ct);
    }

    public async Task<List<EpIssue>> GetProjectIssuesAsync(string baseUrl, string apiKey, int projectId, DateTime? updatedSince, CancellationToken ct)
    {
        // status_id=* is required — without it EasyProject/Redmine returns only OPEN issues,
        // silently dropping all closed tickets (and missing closed-issue updates on incremental).
        var filter = "status_id=*";
        var since = BuildUpdatedSinceFilter(updatedSince);
        if (since != null) filter += $"&{since}";
        return await GetAllPaginatedAsync<EpIssue>(baseUrl, $"projects/{projectId}/issues", apiKey, "issues", ct, filter);
    }

    // EasyProject/Redmine server-side filter for incremental pulls: only issues changed
    // at or after the given instant. The ">=" operator and ":" must be URL-encoded.
    public static string? BuildUpdatedSinceFilter(DateTime? updatedSince)
        => updatedSince is { } since
            ? $"updated_on={Uri.EscapeDataString($">={since.ToUniversalTime():yyyy-MM-ddTHH:mm:ssZ}")}"
            : null;

    public async Task<int> GetProjectIssueCountAsync(string baseUrl, string apiKey, int projectId, CancellationToken ct)
    {
        var url = BuildUrl(baseUrl, $"projects/{projectId}/issues", apiKey, "limit=1&offset=0&status_id=*");
        var response = await httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("total_count", out var tc) ? tc.GetInt32() : 0;
    }

    public async Task<EpIssue> GetIssueDetailAsync(string baseUrl, string apiKey, int issueId, CancellationToken ct)
    {
        var url = BuildUrl(baseUrl, $"issues/{issueId}", apiKey, "include=journals,attachments,easy_checklists");
        var response = await httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var issueElement = doc.RootElement.GetProperty("issue");
        return JsonSerializer.Deserialize<EpIssue>(issueElement.GetRawText(), JsonOptions)!;
    }

    public async Task<List<EpTimeEntry>> GetProjectTimeEntriesAsync(string baseUrl, string apiKey, int projectId, CancellationToken ct)
    {
        return await GetAllPaginatedAsync<EpTimeEntry>(baseUrl, $"projects/{projectId}/time_entries", apiKey, "time_entries", ct);
    }

    public async Task<List<EpUser>> GetUsersAsync(string baseUrl, string apiKey, CancellationToken ct)
    {
        return await GetAllPaginatedAsync<EpUser>(baseUrl, "users", apiKey, "users", ct);
    }

    public async Task<List<EpMembership>> GetProjectMembershipsAsync(string baseUrl, string apiKey, int projectId, CancellationToken ct)
    {
        return await GetAllPaginatedAsync<EpMembership>(baseUrl, $"projects/{projectId}/memberships", apiKey, "memberships", ct);
    }

    public async Task<List<EpTracker>> GetTrackersAsync(string baseUrl, string apiKey, CancellationToken ct)
    {
        var url = BuildUrl(baseUrl, "trackers", apiKey);
        var response = await httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var arr = doc.RootElement.GetProperty("trackers");
        return JsonSerializer.Deserialize<List<EpTracker>>(arr.GetRawText(), JsonOptions) ?? [];
    }

    public async Task<List<EpIssueStatus>> GetIssueStatusesAsync(string baseUrl, string apiKey, CancellationToken ct)
    {
        var url = BuildUrl(baseUrl, "issue_statuses", apiKey);
        var response = await httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var arr = doc.RootElement.GetProperty("issue_statuses");
        return JsonSerializer.Deserialize<List<EpIssueStatus>>(arr.GetRawText(), JsonOptions) ?? [];
    }

    public async Task<List<EpIssuePriority>> GetIssuePrioritiesAsync(string baseUrl, string apiKey, CancellationToken ct)
    {
        var url = BuildUrl(baseUrl, "enumerations/issue_priorities", apiKey);
        var response = await httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var arr = doc.RootElement.GetProperty("issue_priorities");
        return JsonSerializer.Deserialize<List<EpIssuePriority>>(arr.GetRawText(), JsonOptions) ?? [];
    }

    public async Task<List<EpCustomFieldDefinition>> GetCustomFieldsAsync(string baseUrl, string apiKey, CancellationToken ct)
    {
        var url = BuildUrl(baseUrl, "custom_fields", apiKey);
        var response = await httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var arr = doc.RootElement.GetProperty("custom_fields");
        return JsonSerializer.Deserialize<List<EpCustomFieldDefinition>>(arr.GetRawText(), JsonOptions) ?? [];
    }

    public async Task<Stream> DownloadAttachmentAsync(string baseUrl, string apiKey, string contentUrl, CancellationToken ct)
    {
        var separator = contentUrl.Contains('?') ? "&" : "?";
        var url = $"{contentUrl}{separator}key={apiKey}";
        var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync(ct);
    }

    private async Task<List<T>> GetAllPaginatedAsync<T>(
        string baseUrl, string endpoint, string apiKey, string rootProperty, CancellationToken ct, string? extraFilter = null)
    {
        var all = new List<T>();
        var offset = 0;
        const int limit = 100;

        while (true)
        {
            var paging = $"limit={limit}&offset={offset}";
            var url = BuildUrl(baseUrl, endpoint, apiKey, extraFilter is null ? paging : $"{paging}&{extraFilter}");
            var response = await httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty(rootProperty, out var arr))
            {
                var items = JsonSerializer.Deserialize<List<T>>(arr.GetRawText(), JsonOptions) ?? [];
                all.AddRange(items);

                if (items.Count < limit) break;
            }
            else
            {
                break;
            }

            var totalCount = doc.RootElement.TryGetProperty("total_count", out var tc) ? tc.GetInt32() : 0;
            offset += limit;
            if (offset >= totalCount) break;

            // Passive throttling between pages
            await Task.Delay(500, ct);
        }

        return all;
    }

    private static string BuildUrl(string baseUrl, string endpoint, string apiKey, string? extraParams = null)
    {
        var trimmedBase = baseUrl.TrimEnd('/');
        var url = $"{trimmedBase}/{endpoint}.json?key={apiKey}";
        if (!string.IsNullOrEmpty(extraParams))
            url += $"&{extraParams}";
        return url;
    }
}
