using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SoftimProject.Application.Features.Migration.EasyProject.Models;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;
using SoftimProject.Infrastructure.Services.EasyProject;
using SoftimProject.Infrastructure.Services.Redmine;

namespace SoftimProject.Infrastructure.Tests;

public class RedmineFamilyConnectorTests
{
    [Fact]
    public void SourceSystem_DistinguishesProviders()
    {
        var api = new FakeApiClient();
        new EasyProjectSourceConnector(api, NullLogger<EasyProjectSourceConnector>.Instance).SourceSystem.Should().Be(SyncType.EasyProject);
        new RedmineSourceConnector(api, NullLogger<RedmineSourceConnector>.Instance).SourceSystem.Should().Be(SyncType.Redmine);
    }

    [Fact]
    public async Task Redmine_GetProjects_MapsThroughSharedPath()
    {
        var api = new FakeApiClient { Projects = [new EpProject(50, "Web", "<p>desc</p>", 1, null, null, null, null)] };
        var connector = new RedmineSourceConnector(api, NullLogger<RedmineSourceConnector>.Instance);

        var projects = await connector.GetProjectsAsync(new SourceConnectionContext("https://redmine.example", "key"), CancellationToken.None);

        projects.Should().ContainSingle();
        projects[0].ExternalId.Should().Be("50");
        projects[0].Name.Should().Be("Web");
    }

    private sealed class FakeApiClient : IEasyProjectApiClient
    {
        public List<EpProject> Projects { get; init; } = [];

        public Task<(bool Success, string? Error)> TestConnectionAsync(string baseUrl, string apiKey, CancellationToken ct) => Task.FromResult((true, (string?)null));
        public Task<List<EpProject>> GetProjectsAsync(string baseUrl, string apiKey, CancellationToken ct) => Task.FromResult(Projects);
        public Task<List<EpIssue>> GetProjectIssuesAsync(string baseUrl, string apiKey, int projectId, DateTime? updatedSince, CancellationToken ct) => Task.FromResult(new List<EpIssue>());
        public Task<int> GetProjectIssueCountAsync(string baseUrl, string apiKey, int projectId, CancellationToken ct) => Task.FromResult(0);
        public Task<EpIssue> GetIssueDetailAsync(string baseUrl, string apiKey, int issueId, CancellationToken ct) => throw new NotSupportedException();
        public Task<List<EpTimeEntry>> GetProjectTimeEntriesAsync(string baseUrl, string apiKey, int projectId, CancellationToken ct) => Task.FromResult(new List<EpTimeEntry>());
        public Task<List<EpUser>> GetUsersAsync(string baseUrl, string apiKey, CancellationToken ct) => Task.FromResult(new List<EpUser>());
        public Task<List<EpMembership>> GetProjectMembershipsAsync(string baseUrl, string apiKey, int projectId, CancellationToken ct) => Task.FromResult(new List<EpMembership>());
        public Task<List<EpTracker>> GetTrackersAsync(string baseUrl, string apiKey, CancellationToken ct) => Task.FromResult(new List<EpTracker>());
        public Task<List<EpIssueStatus>> GetIssueStatusesAsync(string baseUrl, string apiKey, CancellationToken ct) => Task.FromResult(new List<EpIssueStatus>());
        public Task<List<EpIssuePriority>> GetIssuePrioritiesAsync(string baseUrl, string apiKey, CancellationToken ct) => Task.FromResult(new List<EpIssuePriority>());
        public Task<List<EpCustomFieldDefinition>> GetCustomFieldsAsync(string baseUrl, string apiKey, CancellationToken ct) => Task.FromResult(new List<EpCustomFieldDefinition>());
        public Task<Stream> DownloadAttachmentAsync(string baseUrl, string apiKey, string contentUrl, CancellationToken ct) => throw new NotSupportedException();
    }
}
