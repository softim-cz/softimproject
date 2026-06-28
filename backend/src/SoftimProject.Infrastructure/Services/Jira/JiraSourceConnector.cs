using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SoftimProject.Application.Integrations;
using SoftimProject.Application.Integrations.Jira;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Infrastructure.Services.Jira;

/// <summary>
/// <see cref="ISourceConnector"/> for Jira Cloud (REST v3). Maps Jira issues/projects/lookups
/// onto the canonical model so the shared <c>SyncEngine</c> imports them like any other source.
/// </summary>
/// <remarks>
/// Auth: the connection's ApiToken is "email:token" (Jira Cloud Basic auth). Description is
/// pulled as HTML via <c>expand=renderedFields</c> so the engine's HtmlToMarkdown applies.
/// Initial scope = projects, lookups, issues (core fields). Comments/worklogs/attachments
/// (ADF + per-issue calls) and user enumeration are follow-ups. NEEDS VALIDATION against a
/// live Jira instance.
/// </remarks>
public sealed class JiraSourceConnector(HttpClient httpClient, ILogger<JiraSourceConnector> logger) : ISourceConnector
{
    private const int PageSize = 50;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public SyncType SourceSystem => SyncType.Jira;

    public async Task<(bool Success, string? Error)> TestConnectionAsync(SourceConnectionContext context, CancellationToken ct)
    {
        try
        {
            using var response = await SendAsync(context, "/rest/api/3/myself", ct);
            return response.IsSuccessStatusCode
                ? (true, null)
                : (false, $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Jira connection test failed for {BaseUrl}", context.BaseUrl);
            return (false, ex.Message);
        }
    }

    public async Task<IReadOnlyList<CanonicalProject>> GetProjectsAsync(SourceConnectionContext context, CancellationToken ct)
    {
        var result = new List<CanonicalProject>();
        var startAt = 0;
        while (true)
        {
            var page = await GetAsync<JiraProjectSearchResponse>(context, $"/rest/api/3/project/search?startAt={startAt}&maxResults={PageSize}", ct);
            var values = page?.Values ?? [];
            result.AddRange(values.Select(JiraCanonicalMapper.MapProject));
            startAt += PageSize;
            if (page is null || values.Count < PageSize || startAt >= page.Total) break;
        }
        return result;
    }

    public Task<IReadOnlyList<CanonicalUser>> GetUsersAsync(SourceConnectionContext context, CancellationToken ct)
        // Jira restricts user enumeration (GDPR); users are resolved via mapping/email instead.
        => Task.FromResult<IReadOnlyList<CanonicalUser>>([]);

    public async Task<CanonicalLookups> GetLookupsAsync(SourceConnectionContext context, CancellationToken ct)
    {
        var issueTypes = await GetAsync<List<JiraNamedEntity>>(context, "/rest/api/3/issuetype", ct) ?? [];
        var statuses = await GetAsync<List<JiraStatus>>(context, "/rest/api/3/status", ct) ?? [];
        var priorities = await GetAsync<List<JiraNamedEntity>>(context, "/rest/api/3/priority", ct) ?? [];
        return JiraCanonicalMapper.MapLookups(issueTypes, statuses, priorities);
    }

    public async Task<IReadOnlyList<CanonicalIssue>> GetIssuesAsync(SourceConnectionContext context, string projectExternalId, DateTime? changedSince, CancellationToken ct)
    {
        var jql = $"project={projectExternalId}";
        if (changedSince is { } since)
            jql += $" AND updated >= \"{since.ToUniversalTime():yyyy/MM/dd HH:mm}\"";
        var encodedJql = Uri.EscapeDataString(jql);

        var result = new List<CanonicalIssue>();
        var startAt = 0;
        while (true)
        {
            var path = $"/rest/api/3/search?jql={encodedJql}&startAt={startAt}&maxResults={PageSize}&expand=renderedFields";
            var page = await GetAsync<JiraSearchResponse>(context, path, ct);
            var issues = page?.Issues ?? [];
            result.AddRange(issues.Select(i => JiraCanonicalMapper.MapIssue(i, context.BaseUrl)));
            startAt += PageSize;
            if (page is null || issues.Count < PageSize || startAt >= page.Total) break;
        }
        return result;
    }

    public Task<IReadOnlyList<CanonicalWorklog>> GetWorklogsAsync(SourceConnectionContext context, string projectExternalId, DateTime? changedSince, CancellationToken ct)
        // Worklogs are per-issue in Jira; syncing them is a follow-up.
        => Task.FromResult<IReadOnlyList<CanonicalWorklog>>([]);

    public Task<Stream> DownloadAttachmentAsync(SourceConnectionContext context, string contentUrl, CancellationToken ct)
        => throw new NotSupportedException("Jira attachment download is not yet implemented.");

    private async Task<T?> GetAsync<T>(SourceConnectionContext context, string path, CancellationToken ct)
    {
        using var response = await SendAsync(context, path, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
    }

    private Task<HttpResponseMessage> SendAsync(SourceConnectionContext context, string path, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{context.BaseUrl.TrimEnd('/')}{path}");
        // Jira Cloud Basic auth: ApiToken carries "email:token".
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes(context.ApiToken));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return httpClient.SendAsync(request, ct);
    }
}
