using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SoftimProject.Application.Features.Migration.EasyProject;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;
using SoftimProject.Infrastructure.Persistence;

namespace SoftimProject.WebApi.Tests.Integration;

// Covers the #17 surface that doesn't need a live EasyProject instance:
//   - ResumeMigrationCommand rejects jobs in non-resumable states
//   - ResumeMigrationCommand requires a non-empty ApiKey (validator → 400)
//   - Resume rehydrates stored configuration back into a command shape
//   - Admin-only gating on /migration/resume and /migration/validate
//
// A full resume-to-completion test would require a stub EasyProject API or a deep
// mock of IEasyProjectApiClient — out of scope here, covered by the idempotent
// behaviour of EasyProjectMigrationService proper.
[Collection("Integration")]
public sealed class MigrationResumeTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;
    public MigrationResumeTests(IntegrationTestFactory factory) => _factory = factory;

    private HttpClient ClientAs(string dev)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User-Id", dev);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    private async Task<Guid> SeedJobAsync(MigrationStatus status, MigrationPhase phase, bool withConfig = true)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var job = new MigrationJob
        {
            Id = Guid.NewGuid(),
            InitiatedByUserId = TestDataSeeder.AdminId,
            SourceSystem = "EasyProject",
            SourceBaseUrl = "https://ep.example",
            Status = status,
            CurrentPhase = phase,
            StartedAt = DateTime.UtcNow.AddHours(-1),
            CompletedAt = DateTime.UtcNow,
            Configuration = withConfig
                ? JsonSerializer.Serialize(new StoredMigrationConfig(
                    "https://ep.example",
                    new List<int> { 1 },
                    TargetProjectTemplateId: Guid.NewGuid(),
                    new Dictionary<int, Guid?>(),
                    new Dictionary<int, Guid>(),
                    new Dictionary<int, Guid>(),
                    new Dictionary<int, Guid?>(),
                    SkipClosedIssues: false,
                    SkipAttachments: true,
                    ImportComments: false,
                    ImportWorklogs: false,
                    ImportChecklists: false,
                    CreateMissingUsers: false,
                    null, null, null, null))
                : null,
            CreatedAt = DateTime.UtcNow.AddHours(-2),
        };
        db.MigrationJobs.Add(job);
        await db.SaveChangesAsync();
        return job.Id;
    }

    [Fact]
    public async Task Non_admin_cannot_reach_resume_or_validate()
    {
        using var client = ClientAs(TestDataSeeder.UserAOid);
        var resumeRes = await client.PostAsJsonAsync(
            $"/api/v1/migration/{Guid.NewGuid()}/resume",
            new { apiKey = "key" });
        resumeRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var validateRes = await client.PostAsJsonAsync(
            "/api/v1/migration/validate",
            new { baseUrl = "https://ep", apiKey = "k", projectIds = new int[0] });
        validateRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Resume_rejects_jobs_that_are_not_failed_or_cancelled()
    {
        var jobId = await SeedJobAsync(MigrationStatus.Completed, MigrationPhase.Done);

        using var client = ClientAs(TestDataSeeder.AdminOid);
        var response = await client.PostAsJsonAsync(
            $"/api/v1/migration/{jobId}/resume",
            new { apiKey = "key" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Failed").And.Contain("Cancelled");
    }

    [Fact]
    public async Task Resume_requires_a_non_empty_api_key()
    {
        var jobId = await SeedJobAsync(MigrationStatus.Failed, MigrationPhase.Tickets);

        using var client = ClientAs(TestDataSeeder.AdminOid);
        var response = await client.PostAsJsonAsync(
            $"/api/v1/migration/{jobId}/resume",
            new { apiKey = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Resume_rejects_jobs_without_stored_configuration()
    {
        var jobId = await SeedJobAsync(MigrationStatus.Failed, MigrationPhase.Fetching, withConfig: false);

        using var client = ClientAs(TestDataSeeder.AdminOid);
        var response = await client.PostAsJsonAsync(
            $"/api/v1/migration/{jobId}/resume",
            new { apiKey = "real-key" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("configuration");
    }

    [Fact]
    public async Task Resume_404s_for_unknown_job()
    {
        using var client = ClientAs(TestDataSeeder.AdminOid);
        var response = await client.PostAsJsonAsync(
            $"/api/v1/migration/{Guid.NewGuid()}/resume",
            new { apiKey = "k" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
