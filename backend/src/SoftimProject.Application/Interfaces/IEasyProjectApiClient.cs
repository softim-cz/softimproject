using SoftimProject.Application.Features.Migration.EasyProject.Models;

namespace SoftimProject.Application.Interfaces;

public interface IEasyProjectApiClient
{
    Task<(bool Success, string? Error)> TestConnectionAsync(string baseUrl, string apiKey, CancellationToken ct);
    Task<List<EpProject>> GetProjectsAsync(string baseUrl, string apiKey, CancellationToken ct);
    Task<List<EpIssue>> GetProjectIssuesAsync(string baseUrl, string apiKey, int projectId, CancellationToken ct);
    Task<int> GetProjectIssueCountAsync(string baseUrl, string apiKey, int projectId, CancellationToken ct);
    Task<EpIssue> GetIssueDetailAsync(string baseUrl, string apiKey, int issueId, CancellationToken ct);
    Task<List<EpTimeEntry>> GetProjectTimeEntriesAsync(string baseUrl, string apiKey, int projectId, CancellationToken ct);
    Task<List<EpUser>> GetUsersAsync(string baseUrl, string apiKey, CancellationToken ct);
    Task<List<EpMembership>> GetProjectMembershipsAsync(string baseUrl, string apiKey, int projectId, CancellationToken ct);
    Task<List<EpTracker>> GetTrackersAsync(string baseUrl, string apiKey, CancellationToken ct);
    Task<List<EpIssueStatus>> GetIssueStatusesAsync(string baseUrl, string apiKey, CancellationToken ct);
    Task<List<EpIssuePriority>> GetIssuePrioritiesAsync(string baseUrl, string apiKey, CancellationToken ct);
    Task<List<EpCustomFieldDefinition>> GetCustomFieldsAsync(string baseUrl, string apiKey, CancellationToken ct);
    Task<Stream> DownloadAttachmentAsync(string baseUrl, string apiKey, string contentUrl, CancellationToken ct);
}
