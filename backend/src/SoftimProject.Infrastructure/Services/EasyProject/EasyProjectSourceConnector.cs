using Microsoft.Extensions.Logging;
using SoftimProject.Application.Interfaces;
using SoftimProject.Application.Integrations;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Infrastructure.Services.EasyProject;

/// <summary>
/// <see cref="ISourceConnector"/> for EasyProject. Wraps the existing
/// <see cref="IEasyProjectApiClient"/> and maps its responses onto the canonical model
/// via <see cref="EasyProjectCanonicalMapper"/>. This is the seam future systems
/// (Jira, Redmine) plug into; the shared sync engine consumes only the canonical model.
/// </summary>
public sealed class EasyProjectSourceConnector(
    IEasyProjectApiClient apiClient,
    ILogger<EasyProjectSourceConnector> logger) : ISourceConnector
{
    public SyncType SourceSystem => SyncType.EasyProject;

    public Task<(bool Success, string? Error)> TestConnectionAsync(SourceConnectionContext context, CancellationToken ct)
        => apiClient.TestConnectionAsync(context.BaseUrl, context.ApiToken, ct);

    public async Task<IReadOnlyList<CanonicalUser>> GetUsersAsync(SourceConnectionContext context, CancellationToken ct)
    {
        var users = await apiClient.GetUsersAsync(context.BaseUrl, context.ApiToken, ct);
        return users.Select(EasyProjectCanonicalMapper.MapUser).ToList();
    }

    public async Task<CanonicalLookups> GetLookupsAsync(SourceConnectionContext context, CancellationToken ct)
    {
        var trackers = await apiClient.GetTrackersAsync(context.BaseUrl, context.ApiToken, ct);
        var statuses = await apiClient.GetIssueStatusesAsync(context.BaseUrl, context.ApiToken, ct);
        var priorities = await apiClient.GetIssuePrioritiesAsync(context.BaseUrl, context.ApiToken, ct);
        return EasyProjectCanonicalMapper.MapLookups(trackers, statuses, priorities);
    }

    public async Task<IReadOnlyList<CanonicalProject>> GetProjectsAsync(SourceConnectionContext context, CancellationToken ct)
    {
        var definitions = await apiClient.GetCustomFieldsAsync(context.BaseUrl, context.ApiToken, ct);
        var optionsFor = EasyProjectCanonicalMapper.BuildOptionsResolver(definitions);
        var projects = await apiClient.GetProjectsAsync(context.BaseUrl, context.ApiToken, ct);
        return projects.Select(p => EasyProjectCanonicalMapper.MapProject(p, optionsFor)).ToList();
    }

    public async Task<IReadOnlyList<CanonicalIssue>> GetIssuesAsync(SourceConnectionContext context, string projectExternalId, CancellationToken ct)
    {
        var epProjectId = ParseProjectId(projectExternalId);
        var definitions = await apiClient.GetCustomFieldsAsync(context.BaseUrl, context.ApiToken, ct);
        var optionsFor = EasyProjectCanonicalMapper.BuildOptionsResolver(definitions);

        var issues = await apiClient.GetProjectIssuesAsync(context.BaseUrl, context.ApiToken, epProjectId, ct);
        var result = new List<CanonicalIssue>(issues.Count);
        foreach (var issue in issues)
        {
            ct.ThrowIfCancellationRequested();
            // Detail call brings journals/attachments/checklists; on failure fall back to
            // the list payload (best-effort), matching the existing migration behavior.
            var detail = issue;
            try
            {
                detail = await apiClient.GetIssueDetailAsync(context.BaseUrl, context.ApiToken, issue.Id, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to fetch issue detail #{IssueId}; using list payload", issue.Id);
            }

            result.Add(EasyProjectCanonicalMapper.MapIssue(detail, optionsFor));
        }

        return result;
    }

    public async Task<IReadOnlyList<CanonicalWorklog>> GetWorklogsAsync(SourceConnectionContext context, string projectExternalId, CancellationToken ct)
    {
        var epProjectId = ParseProjectId(projectExternalId);
        var entries = await apiClient.GetProjectTimeEntriesAsync(context.BaseUrl, context.ApiToken, epProjectId, ct);
        return entries.Select(EasyProjectCanonicalMapper.MapWorklog).ToList();
    }

    public Task<Stream> DownloadAttachmentAsync(SourceConnectionContext context, string contentUrl, CancellationToken ct)
        => apiClient.DownloadAttachmentAsync(context.BaseUrl, context.ApiToken, contentUrl, ct);

    private static int ParseProjectId(string projectExternalId) =>
        int.TryParse(projectExternalId, out var id)
            ? id
            : throw new ArgumentException(
                $"EasyProject project id must be numeric, got '{projectExternalId}'.",
                nameof(projectExternalId));
}
