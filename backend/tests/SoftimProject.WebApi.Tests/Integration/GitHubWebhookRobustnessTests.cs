using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;
using SoftimProject.Infrastructure.Persistence;

namespace SoftimProject.WebApi.Tests.Integration;

// #111 — PR enrichment (description, commit count, CI checks) + webhook robustness
// (delivery-id idempotency, dead-letter on failure, replay).
[Collection("Integration")]
public sealed class GitHubWebhookRobustnessTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public GitHubWebhookRobustnessTests(IntegrationTestFactory factory) => _factory = factory;

    private HttpClient Client()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    private async Task<(string repo, string code, Guid ticketId, int number)> SeedProjectWithTicketAsync(string prefix, int ticketNumber = 42)
    {
        var code = $"{prefix}{DateTime.UtcNow.Ticks % 100000}";
        var repo = $"softim/{code.ToLowerInvariant()}";
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (!await db.TicketPriorities.AnyAsync(tp => tp.IsActive))
            db.TicketPriorities.Add(new TicketPriority { Id = Guid.NewGuid(), Name = "Medium", SortOrder = 1, IsActive = true, IsDefault = true, CreatedAt = DateTime.UtcNow });
        if (!await db.TaskStates.AnyAsync(ts => ts.IsActive && !ts.IsClosedState))
            db.TaskStates.Add(new TaskState { Id = Guid.NewGuid(), Name = "To Do", SortOrder = 1, IsActive = true, IsDefault = true, IsClosedState = false, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var priorityId = await db.TicketPriorities.Where(tp => tp.IsActive).Select(tp => tp.Id).FirstAsync();
        var stateId = await db.TaskStates.Where(ts => ts.IsActive && !ts.IsClosedState).Select(ts => ts.Id).FirstAsync();

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = $"Proj {code}",
            Status = ProjectStatus.Active,
            ExternalSystem = "GitHub",
            ExternalProjectId = repo,
            CreatedAt = DateTime.UtcNow,
        };
        db.Projects.Add(project);
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Number = ticketNumber,
            Title = "Demo",
            TicketPriorityId = priorityId,
            TaskStateId = stateId,
            ReporterId = TestDataSeeder.AdminId,
            Position = 0,
            CreatedAt = DateTime.UtcNow,
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();
        return (repo, code, ticket.Id, ticket.Number);
    }

    private static HttpRequestMessage WebhookRequest(string eventType, string payload, string? deliveryId = null)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/github")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        req.Headers.Add("X-GitHub-Event", eventType);
        if (deliveryId != null) req.Headers.Add("X-GitHub-Delivery", deliveryId);
        return req;
    }

    [Fact]
    public async Task Pull_request_webhook_stores_description_and_commit_count()
    {
        var (repo, code, ticketId, number) = await SeedProjectWithTicketAsync("E");

        var payload = $$"""
            {
              "action": "opened",
              "repository": { "full_name": "{{repo}}" },
              "pull_request": {
                "number": 601,
                "title": "Implement {{code}}-{{number}}",
                "html_url": "https://github.com/{{repo}}/pull/601",
                "head": { "ref": "feat/{{code}}-{{number}}-x" },
                "body": "This PR closes the ticket.\nDetails here.",
                "commits": 5,
                "merged": false,
                "user": { "login": "alice" },
                "created_at": "2026-06-15T10:00:00Z"
              }
            }
            """;

        using var client = Client();
        var response = await client.SendAsync(WebhookRequest("pull_request", payload));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var pr = await db.LinkedPullRequests.FirstOrDefaultAsync(lp => lp.TicketId == ticketId && lp.ExternalId == "601");
        pr.Should().NotBeNull();
        pr!.Description.Should().Contain("closes the ticket");
        pr.CommitsCount.Should().Be(5);
    }

    [Fact]
    public async Task Check_suite_webhook_sets_checks_status_on_linked_pr()
    {
        var (repo, code, ticketId, number) = await SeedProjectWithTicketAsync("C");

        var prPayload = $$"""
            {
              "action": "opened",
              "repository": { "full_name": "{{repo}}" },
              "pull_request": {
                "number": 700, "title": "PR {{code}}-{{number}}",
                "html_url": "https://github.com/{{repo}}/pull/700",
                "head": { "ref": "feat/{{code}}-{{number}}-c" },
                "body": null, "commits": 2, "merged": false,
                "user": { "login": "bob" }, "created_at": "2026-06-15T10:00:00Z"
              }
            }
            """;
        using var client = Client();
        (await client.SendAsync(WebhookRequest("pull_request", prPayload))).StatusCode.Should().Be(HttpStatusCode.OK);

        var checkPayload = $$"""
            {
              "action": "completed",
              "repository": { "full_name": "{{repo}}" },
              "check_suite": {
                "status": "completed",
                "conclusion": "success",
                "pull_requests": [ { "number": 700 } ]
              }
            }
            """;
        (await client.SendAsync(WebhookRequest("check_suite", checkPayload))).StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var pr = await db.LinkedPullRequests.FirstOrDefaultAsync(lp => lp.TicketId == ticketId && lp.ExternalId == "700");
        pr!.ChecksStatus.Should().Be("success");
    }

    [Fact]
    public async Task Duplicate_delivery_id_is_ignored()
    {
        var (repo, _, _, _) = await SeedProjectWithTicketAsync("D");
        var deliveryId = $"dup-{Guid.NewGuid()}";
        var payload = $$"""
            { "action": "completed", "repository": { "full_name": "{{repo}}" },
              "check_suite": { "status": "completed", "conclusion": "success", "pull_requests": [] } }
            """;

        using var client = Client();
        var first = await client.SendAsync(WebhookRequest("check_suite", payload, deliveryId));
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await client.SendAsync(WebhookRequest("check_suite", payload, deliveryId));
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondBody = await second.Content.ReadAsStringAsync();
        secondBody.Should().Contain("Duplicate delivery ignored");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var count = await db.ProcessedWebhookDeliveries.CountAsync(d => d.DeliveryId == deliveryId);
        count.Should().Be(1, "the delivery is recorded exactly once");
    }

    [Fact]
    public async Task Malformed_event_is_dead_lettered_and_returns_202()
    {
        var (repo, _, _, _) = await SeedProjectWithTicketAsync("F");
        var deliveryId = $"fail-{Guid.NewGuid()}";
        // 'issues' event whose payload is missing the required "issue" object → processor throws.
        var payload = $$"""
            { "action": "opened", "repository": { "full_name": "{{repo}}" } }
            """;

        using var client = Client();
        var response = await client.SendAsync(WebhookRequest("issues", payload, deliveryId));
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var entry = await db.DeadLetterEntries
            .FirstOrDefaultAsync(e => e.OperationType == DeadLetterOperation.GitHubWebhook && e.OperationKey == deliveryId);
        entry.Should().NotBeNull("a failed webhook must be parked in the DLQ for replay");
    }

    [Fact]
    public async Task Replaying_a_github_webhook_dead_letter_applies_the_event()
    {
        var (repo, code, ticketId, number) = await SeedProjectWithTicketAsync("R");

        var body = $$"""
            {
              "action": "opened",
              "repository": { "full_name": "{{repo}}" },
              "pull_request": {
                "number": 808, "title": "PR {{code}}-{{number}}",
                "html_url": "https://github.com/{{repo}}/pull/808",
                "head": { "ref": "feat/{{code}}-{{number}}-replay" },
                "body": "replayed", "commits": 3, "merged": false,
                "user": { "login": "carol" }, "created_at": "2026-06-15T10:00:00Z"
              }
            }
            """;
        var storedPayload = JsonSerializer.Serialize(new { EventType = "pull_request", Body = body });

        Guid entryId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var entry = new DeadLetterEntry
            {
                Id = Guid.NewGuid(),
                OperationType = DeadLetterOperation.GitHubWebhook,
                OperationKey = $"replay-{Guid.NewGuid()}",
                Payload = storedPayload,
                AttemptCount = 1,
                LastError = "boom",
                FirstFailedAt = DateTime.UtcNow,
                LastFailedAt = DateTime.UtcNow,
                Status = DeadLetterStatus.Pending,
            };
            db.DeadLetterEntries.Add(entry);
            await db.SaveChangesAsync();
            entryId = entry.Id;
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var replayer = scope.ServiceProvider.GetRequiredService<IDeadLetterReplayer>();
            var entry = await db.DeadLetterEntries.FindAsync(entryId);
            var outcome = await replayer.ReplayAsync(entry!);
            outcome.Succeeded.Should().BeTrue(outcome.ErrorMessage);
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var pr = await db.LinkedPullRequests.FirstOrDefaultAsync(lp => lp.TicketId == ticketId && lp.ExternalId == "808");
            pr.Should().NotBeNull("replaying the dead-lettered webhook must re-apply the PR link");
            pr!.CommitsCount.Should().Be(3);
        }
    }
}
