using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoftimProject.Application.Features.Admin;
using SoftimProject.Application.Features.Tickets.AiHistory;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;
using SoftimProject.Infrastructure.Persistence;

namespace SoftimProject.WebApi.Tests.Integration;

// Covers #16 AI audit surface:
//   - Recorder writes one AiInvocation row per logical call with hash + cost + duration
//   - Recorder throws AiRateLimitExceededException once a user crosses the window limit
//     (→ middleware maps to HTTP 429)
//   - GET /tickets/{id}/ai/invocations returns the history newest-first
//   - POST /tickets/{id}/ai/resummarize requires a reason (empty → 400), records Manual
//   - Admin GET /admin/ai-usage aggregates totals by project
//
// The AI provider itself (Microsoft.Extensions.AI IChatClient) isn't registered in the
// test factory, so the real Azure OpenAI call short-circuits — AiService returns empty
// strings + zero usage. That's deliberate: tests focus on the audit path, not the LLM.
[Collection("Integration")]
public sealed class AiAuditTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public AiAuditTests(IntegrationTestFactory factory) => _factory = factory;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private HttpClient ClientAs(string devUserId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User-Id", devUserId);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    private async Task<(Guid ProjectId, Guid TicketId)> CreateTicketAsync(string projectCode)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (!await db.TicketPriorities.AnyAsync())
        {
            db.TicketPriorities.Add(new TicketPriority
            {
                Id = Guid.NewGuid(),
                Name = "Medium",
                SortOrder = 1,
                IsActive = true,
                IsDefault = true,
                CreatedAt = DateTime.UtcNow,
            });
        }
        if (!await db.TaskStates.AnyAsync(ts => ts.IsActive && !ts.IsClosedState))
        {
            db.TaskStates.Add(new TaskState
            {
                Id = Guid.NewGuid(),
                Name = "To Do",
                IsActive = true,
                IsDefault = true,
                IsClosedState = false,
                SortOrder = 1,
                CreatedAt = DateTime.UtcNow,
            });
        }
        await db.SaveChangesAsync();

        var priorityId = await db.TicketPriorities.Where(tp => tp.IsActive).Select(tp => tp.Id).FirstAsync();
        var stateId = await db.TaskStates.Where(ts => ts.IsActive && !ts.IsClosedState).Select(ts => ts.Id).FirstAsync();

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Code = projectCode,
            Name = $"Proj {projectCode}",
            Status = ProjectStatus.Active,
            CreatedAt = DateTime.UtcNow,
        };
        db.Projects.Add(project);
        db.ProjectMembers.Add(new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = TestDataSeeder.UserAId,
            Role = ProjectRole.Developer,
            JoinedAt = DateTime.UtcNow,
        });
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Number = 1,
            Title = "Test ticket",
            TicketPriorityId = priorityId,
            TaskStateId = stateId,
            ReporterId = TestDataSeeder.AdminId,
            Position = 0,
            CreatedAt = DateTime.UtcNow,
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();
        return (project.Id, ticket.Id);
    }

    [Fact]
    public async Task Recorder_writes_one_row_per_call_with_hash_and_cost_estimate()
    {
        var (projectId, ticketId) = await CreateTicketAsync($"A{DateTime.UtcNow.Ticks % 100000}");

        using var scope = _factory.Services.CreateScope();
        var recorder = scope.ServiceProvider.GetRequiredService<IAiInvocationRecorder>();

        var recorded = await recorder.RecordAsync<string>(
            new AiInvocationContext(
                AiInvocationTrigger.AutoSummarize,
                InputText: "hello|world",
                TriggeredByUserId: null,
                ProjectId: projectId,
                TicketId: ticketId),
            _ => Task.FromResult(new AiInvocationCall<string>("summary", 100, 50, "summary")),
            CancellationToken.None);

        recorded.Payload.Should().Be("summary");

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var row = await db.AiInvocations.FirstOrDefaultAsync(x => x.Id == recorded.InvocationId);
        row.Should().NotBeNull();
        row!.PromptTokens.Should().Be(100);
        row.CompletionTokens.Should().Be(50);
        row.TotalTokens.Should().Be(150);
        // 100 input * $2.5 / 1M + 50 output * $10 / 1M = 0.00025 + 0.0005 = 0.00075
        row.EstimatedCostUsd.Should().BeApproximately(0.00075m, 0.000001m);
        row.InputHash.Should().NotBeNullOrEmpty();
        row.Success.Should().BeTrue();
        row.Trigger.Should().Be(AiInvocationTrigger.AutoSummarize);
    }

    [Fact]
    public async Task Recorder_trips_rate_limit_after_window_threshold()
    {
        var (projectId, ticketId) = await CreateTicketAsync($"R{DateTime.UtcNow.Ticks % 100000}");
        var userId = TestDataSeeder.UserAId;

        using var scope = _factory.Services.CreateScope();
        var recorder = scope.ServiceProvider.GetRequiredService<IAiInvocationRecorder>();

        // Default config: 20 calls per 10-minute window. Pre-seed 20 rows in the window,
        // then the 21st call should throw AiRateLimitExceededException.
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTime.UtcNow;
        for (var i = 0; i < 20; i++)
        {
            db.AiInvocations.Add(new AiInvocation
            {
                Id = Guid.NewGuid(),
                Trigger = AiInvocationTrigger.ManualResummarize,
                TriggeredByUserId = userId,
                ProjectId = projectId,
                TicketId = ticketId,
                InputHash = $"{i:x}",
                Model = "gpt-4o",
                StartedAt = now.AddMinutes(-1),
                CompletedAt = now.AddMinutes(-1),
                Success = true,
            });
        }
        await db.SaveChangesAsync();

        var act = () => recorder.RecordAsync<string>(
            new AiInvocationContext(
                AiInvocationTrigger.ManualResummarize,
                InputText: "x",
                TriggeredByUserId: userId,
                ProjectId: projectId,
                TicketId: ticketId,
                Reason: "retry"),
            _ => Task.FromResult(new AiInvocationCall<string>("s", 1, 1, "s")),
            CancellationToken.None);

        await act.Should().ThrowAsync<AiRateLimitExceededException>()
            .Where(e => e.Message.Contains("rate limit"));
    }

    [Fact]
    public async Task Resummarize_endpoint_requires_a_non_empty_reason()
    {
        var (projectId, ticketId) = await CreateTicketAsync($"B{DateTime.UtcNow.Ticks % 100000}");
        using var client = ClientAs(TestDataSeeder.AdminOid);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/projects/{projectId}/tickets/{ticketId}/ai/resummarize",
            new { reason = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Resummarize_endpoint_records_manual_trigger_with_user_and_reason()
    {
        var (projectId, ticketId) = await CreateTicketAsync($"C{DateTime.UtcNow.Ticks % 100000}");
        using var client = ClientAs(TestDataSeeder.AdminOid);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/projects/{projectId}/tickets/{ticketId}/ai/resummarize",
            new { reason = "user complained summary is stale" });

        // No AI provider is configured in the test factory, so the summary comes back
        // empty and the handler surfaces a clear error (#84) instead of silently saving
        // an empty run. The manual-trigger invocation is still audited (asserted below).
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var row = await db.AiInvocations
            .OrderByDescending(i => i.StartedAt)
            .FirstOrDefaultAsync(i => i.TicketId == ticketId && i.Trigger == AiInvocationTrigger.ManualResummarize);
        row.Should().NotBeNull();
        row!.Reason.Should().Be("user complained summary is stale");
        row.TriggeredByUserId.Should().Be(TestDataSeeder.AdminId);
    }

    [Fact]
    public async Task Ai_usage_endpoint_aggregates_totals_by_project_for_admin()
    {
        var (projectId, ticketId) = await CreateTicketAsync($"U{DateTime.UtcNow.Ticks % 100000}");
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.AiInvocations.Add(new AiInvocation
        {
            Id = Guid.NewGuid(),
            Trigger = AiInvocationTrigger.AutoSummarize,
            ProjectId = projectId,
            TicketId = ticketId,
            InputHash = "abc",
            Model = "gpt-4o",
            PromptTokens = 500,
            CompletionTokens = 200,
            TotalTokens = 700,
            EstimatedCostUsd = 0.00325m,
            StartedAt = DateTime.UtcNow.AddMinutes(-5),
            CompletedAt = DateTime.UtcNow.AddMinutes(-5),
            Success = true,
        });
        await db.SaveChangesAsync();

        using var client = ClientAs(TestDataSeeder.AdminOid);
        var response = await client.GetAsync("/api/v1/admin/ai-usage?days=30");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AiUsageDto>(JsonOptions);
        body.Should().NotBeNull();
        body!.ByProject.Should().Contain(p => p.ProjectId == projectId && p.TotalTokens >= 700);
    }

    [Fact]
    public async Task Non_admin_cannot_read_ai_usage()
    {
        using var client = ClientAs(TestDataSeeder.UserAOid);
        var response = await client.GetAsync("/api/v1/admin/ai-usage");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
