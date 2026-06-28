using Microsoft.Extensions.Logging;
using SoftimProject.Application.Integrations;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;
using SoftimProject.Infrastructure.Services.EasyProject;

namespace SoftimProject.Infrastructure.Services.Integrations;

/// <summary>
/// Shared <see cref="ISourceConnector"/> for Redmine-family systems. EasyProject is a Redmine
/// derivative, so both speak the same REST shape (<see cref="IEasyProjectApiClient"/>) and map
/// through the same <see cref="EasyProjectCanonicalMapper"/>; only <see cref="SourceSystem"/>
/// differs. Vanilla Redmine simply omits EasyProject-only fields (easy_checklists,
/// easy_is_billable) — they map to empty/false.
/// </summary>
public abstract class RedmineFamilyConnector(IEasyProjectApiClient apiClient, ILogger logger) : ISourceConnector
{
    public abstract SyncType SourceSystem { get; }

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

    public async Task<IReadOnlyList<CanonicalIssue>> GetIssuesAsync(SourceConnectionContext context, string projectExternalId, DateTime? changedSince, CancellationToken ct)
    {
        var projectId = ParseProjectId(projectExternalId);
        var definitions = await apiClient.GetCustomFieldsAsync(context.BaseUrl, context.ApiToken, ct);
        var optionsFor = EasyProjectCanonicalMapper.BuildOptionsResolver(definitions);

        var issues = await apiClient.GetProjectIssuesAsync(context.BaseUrl, context.ApiToken, projectId, changedSince, ct);
        var result = new List<CanonicalIssue>(issues.Count);
        foreach (var issue in issues)
        {
            ct.ThrowIfCancellationRequested();
            var detail = issue;
            try
            {
                detail = await apiClient.GetIssueDetailAsync(context.BaseUrl, context.ApiToken, issue.Id, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to fetch issue detail #{IssueId}; using list payload", issue.Id);
            }

            var canonical = EasyProjectCanonicalMapper.MapIssue(detail, optionsFor)
                with
            { WebUrl = $"{context.BaseUrl.TrimEnd('/')}/issues/{detail.Id}" };
            result.Add(canonical);
        }

        return result;
    }

    public async Task<IReadOnlyList<CanonicalWorklog>> GetWorklogsAsync(SourceConnectionContext context, string projectExternalId, DateTime? changedSince, CancellationToken ct)
    {
        // Redmine time_entries don't expose a reliable updated_on filter, so worklogs are
        // pulled in full; the engine's ExternalId upsert dedups them.
        _ = changedSince;
        var projectId = ParseProjectId(projectExternalId);
        var entries = await apiClient.GetProjectTimeEntriesAsync(context.BaseUrl, context.ApiToken, projectId, ct);
        return entries.Select(EasyProjectCanonicalMapper.MapWorklog).ToList();
    }

    public Task<Stream> DownloadAttachmentAsync(SourceConnectionContext context, string contentUrl, CancellationToken ct)
        => apiClient.DownloadAttachmentAsync(context.BaseUrl, context.ApiToken, contentUrl, ct);

    private static int ParseProjectId(string projectExternalId) =>
        int.TryParse(projectExternalId, out var id)
            ? id
            : throw new ArgumentException(
                $"Project id must be numeric, got '{projectExternalId}'.",
                nameof(projectExternalId));
}
