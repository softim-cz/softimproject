using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SoftimProject.Application.Features.Health;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;
using SoftimProject.Infrastructure.Persistence;
using SoftimProject.Infrastructure.Services;

namespace SoftimProject.WebApi.Tests.Integration;

// Covers the #12 observability surface:
//   - JobRunRecorder persists a Running row on Begin and updates it to a final status on dispose
//   - GET /api/v1/health/jobs returns Healthy when every registered job has a recent successful
//     run, Degraded (503) when a job is overdue or its last run Failed
//
// Registrations and runs are injected via IJobRegistry / the DbContext directly; we don't wait
// for the PeriodicTimer tick because these tests exercise the contract, not the scheduler.
[Collection("Integration")]
public sealed class JobsHealthTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public JobsHealthTests(IntegrationTestFactory factory) => _factory = factory;

    // Mirrors the Web API's AddJsonOptions so enum properties round-trip as strings.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private HttpClient AnonymousClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    [Fact]
    public async Task Recorder_persists_running_row_on_begin_and_final_status_on_dispose()
    {
        using var scope = _factory.Services.CreateScope();
        var recorder = scope.ServiceProvider.GetRequiredService<IJobRunRecorder>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Guid runId;
        await using (var run = await recorder.BeginAsync("TestJob"))
        {
            runId = run.JobRunId;
            var mid = await db.JobRuns.FindAsync(runId);
            mid.Should().NotBeNull();
            mid!.Status.Should().Be(JobRunStatus.Running);
            mid.CompletedAt.Should().BeNull();

            run.MarkSuccess(itemsProcessed: 7);
        }

        db.ChangeTracker.Clear();
        var final = await db.JobRuns.FindAsync(runId);
        final.Should().NotBeNull();
        final!.Status.Should().Be(JobRunStatus.Success);
        final.CompletedAt.Should().NotBeNull();
        final.ItemsProcessed.Should().Be(7);
        final.DurationMs.Should().NotBeNull();
    }

    [Fact]
    public async Task Recorder_marks_run_as_failed_when_scope_disposes_without_outcome()
    {
        using var scope = _factory.Services.CreateScope();
        var recorder = scope.ServiceProvider.GetRequiredService<IJobRunRecorder>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Guid runId;
        await using (var run = await recorder.BeginAsync("ForgetfulJob"))
        {
            runId = run.JobRunId;
            // No MarkSuccess/Partial/Failure — should default to Failed on dispose.
        }

        db.ChangeTracker.Clear();
        var final = await db.JobRuns.FindAsync(runId);
        final!.Status.Should().Be(JobRunStatus.Failed);
        final.ErrorMessage.Should().Contain("without an explicit outcome");
    }

    // Notes: IJobRegistry is a singleton owned by the WebApplicationFactory, so registrations
    // accumulate across tests in this collection. Each test uses a unique job name and only
    // asserts on its own row rather than the overall Status, which depends on whatever other
    // jobs a prior test registered.

    [Fact]
    public async Task Jobs_health_reports_a_recent_run_as_not_overdue_with_the_persisted_counters()
    {
        var jobName = $"HealthyJob_{Guid.NewGuid():N}";
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IJobRegistry>()
            .Register(jobName, TimeSpan.FromHours(1));

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.JobRuns.Add(new JobRun
        {
            Id = Guid.NewGuid(),
            JobName = jobName,
            StartedAt = DateTime.UtcNow.AddMinutes(-5),
            CompletedAt = DateTime.UtcNow.AddMinutes(-5).AddSeconds(2),
            Status = JobRunStatus.Success,
            DurationMs = 2000,
            ItemsProcessed = 1,
        });
        await db.SaveChangesAsync();

        using var client = AnonymousClient();
        var response = await client.GetAsync("/api/v1/health/jobs");

        var body = await response.Content.ReadFromJsonAsync<JobsHealthDto>(JsonOptions);
        var job = body!.Jobs.Single(j => j.JobName == jobName);
        job.IsOverdue.Should().BeFalse();
        job.LastStatus.Should().Be(JobRunStatus.Success);
        job.LastItemsProcessed.Should().Be(1);
        job.LastDurationMs.Should().Be(2000);
    }

    [Fact]
    public async Task Jobs_health_flags_job_as_overdue_and_endpoint_returns_503_when_any_job_is_stale()
    {
        var jobName = $"StuckJob_{Guid.NewGuid():N}";
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IJobRegistry>()
            .Register(jobName, TimeSpan.FromMinutes(5));

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        // Last run older than 2× the expected interval — should trip the overdue check.
        db.JobRuns.Add(new JobRun
        {
            Id = Guid.NewGuid(),
            JobName = jobName,
            StartedAt = DateTime.UtcNow.AddHours(-1),
            CompletedAt = DateTime.UtcNow.AddHours(-1).AddSeconds(1),
            Status = JobRunStatus.Success,
            DurationMs = 1000,
        });
        await db.SaveChangesAsync();

        using var client = AnonymousClient();
        var response = await client.GetAsync("/api/v1/health/jobs");

        // Any overdue registered job degrades the whole endpoint to 503.
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var body = await response.Content.ReadFromJsonAsync<JobsHealthDto>(JsonOptions);
        body!.Status.Should().Be("Degraded");
        body.Jobs.Single(j => j.JobName == jobName).IsOverdue.Should().BeTrue();
    }
}
