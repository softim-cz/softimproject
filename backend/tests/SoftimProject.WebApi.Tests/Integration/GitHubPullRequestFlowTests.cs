using System.Net;
using System.Net.Http.Headers;
using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;
using SoftimProject.Infrastructure.Persistence;
using SoftimProject.Infrastructure.Services;

namespace SoftimProject.WebApi.Tests.Integration;

// Exercises the #15 GitHub E2E surface without talking to GitHub:
//   - The ticket resolver picks up (ProjectCode, Number) from branch / title / body
//   - The /api/webhooks/github endpoint, given a pull_request event whose branch name
//     matches the ticket key, upserts a LinkedPullRequest and applies the convention
//     status transition (opened → "In Review" state, merged → closed state)
//   - The /api/v1/projects/{id}/tickets/{id}/github/pull-requests query returns the row
//
// The actual Octokit-backed Create Branch command is not covered here because that
// requires a live GitHub call; it's smoke-tested manually via the UI. Unit coverage
// for the name slugify / key pattern lives in this suite via GitHubTicketResolver.
[Collection("Integration")]
public sealed class GitHubPullRequestFlowTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public GitHubPullRequestFlowTests(IntegrationTestFactory factory) => _factory = factory;

    private HttpClient Client()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    private HttpClient ClientAs(string dev)
    {
        var client = Client();
        client.DefaultRequestHeaders.Add("X-Dev-User-Id", dev);
        return client;
    }

    [Theory]
    [InlineData("feat/PRJA-12-add-logging", "PRJA", 12)]
    [InlineData("bugfix/prja-7-crashfix", "PRJA", 7)] // lowercase collapses via ToUpper
    [InlineData("Fixes PRJA-99 in prod", "PRJA", 99)]
    [InlineData("chore(PRJA-3): tidy", "PRJA", 3)]
    public void TicketResolver_picks_the_first_matching_project_and_number(string input, string code, int number)
    {
        var key = GitHubTicketResolver.TryResolve(input);
        key.Should().NotBeNull();
        key!.ProjectCode.Should().Be(code);
        key.TicketNumber.Should().Be(number);
    }

    [Theory]
    [InlineData("main")]
    [InlineData("feat/some-description")]
    [InlineData("v1.0-release")]
    public void TicketResolver_returns_null_for_branches_without_a_key(string input)
    {
        GitHubTicketResolver.TryResolve(input).Should().BeNull();
    }

    // TestDataSeeder doesn't seed TaskStates/TicketPriorities (its remit is users +
    // projects). Each test that needs a ticket ensures at least one priority + a
    // non-closed default state exists.
    private static async Task EnsureLookupsAsync(ApplicationDbContext db)
    {
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
                SortOrder = 1,
                IsActive = true,
                IsDefault = true,
                IsClosedState = false,
                CreatedAt = DateTime.UtcNow,
            });
        }
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Pull_request_opened_webhook_upserts_linked_pr_and_moves_ticket_to_review_state()
    {
        // Arrange: project linked to a GitHub repo, a ticket, and a "In Review" TaskState.
        var projectCode = $"W{DateTime.UtcNow.Ticks % 100000}";
        var repoFullName = $"softim/{projectCode.ToLowerInvariant()}";
        Guid projectId;
        Guid ticketId;
        int ticketNumber;
        Guid reviewStateId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await EnsureLookupsAsync(db);
            var priorityId = await db.TicketPriorities.Where(tp => tp.IsActive).Select(tp => tp.Id).FirstAsync();
            var defaultStateId = await db.TaskStates.Where(ts => ts.IsActive && !ts.IsClosedState).Select(ts => ts.Id).FirstAsync();

            reviewStateId = Guid.NewGuid();
            db.TaskStates.Add(new TaskState
            {
                Id = reviewStateId,
                Name = "In Review",
                SortOrder = 50,
                IsActive = true,
                IsDefault = false,
                IsClosedState = false,
                CreatedAt = DateTime.UtcNow,
            });

            var project = new Project
            {
                Id = Guid.NewGuid(),
                Code = projectCode,
                Name = $"Proj {projectCode}",
                Status = ProjectStatus.Active,
                ExternalSystem = "GitHub",
                ExternalProjectId = repoFullName,
                CreatedAt = DateTime.UtcNow,
            };
            db.Projects.Add(project);

            var ticket = new Ticket
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                Number = 42,
                Title = "Demo ticket",
                TicketPriorityId = priorityId,
                TaskStateId = defaultStateId,
                ReporterId = TestDataSeeder.AdminId,
                Position = 0,
                CreatedAt = DateTime.UtcNow,
            };
            db.Tickets.Add(ticket);
            await db.SaveChangesAsync();

            projectId = project.Id;
            ticketId = ticket.Id;
            ticketNumber = ticket.Number;
        }

        var payload = $$"""
            {
              "action": "opened",
              "repository": { "full_name": "{{repoFullName}}" },
              "pull_request": {
                "number": 501,
                "title": "Implement {{projectCode}}-{{ticketNumber}}",
                "html_url": "https://github.com/{{repoFullName}}/pull/501",
                "head": { "ref": "feat/{{projectCode}}-{{ticketNumber}}-demo" },
                "body": null,
                "merged": false,
                "user": { "login": "alice" },
                "created_at": "2026-04-24T10:00:00Z",
                "closed_at": null,
                "merged_at": null
              }
            }
            """;

        using var client = Client();
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/github")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        req.Headers.Add("X-GitHub-Event", "pull_request");
        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var linked = await verifyDb.LinkedPullRequests
            .FirstOrDefaultAsync(lp => lp.TicketId == ticketId && lp.ExternalId == "501");
        linked.Should().NotBeNull();
        linked!.State.Should().Be(PullRequestState.Open);
        linked.Branch.Should().Be($"feat/{projectCode}-{ticketNumber}-demo");

        var ticketAfter = await verifyDb.Tickets.FindAsync(ticketId);
        ticketAfter!.TaskStateId.Should().Be(reviewStateId,
            "PR opened should transition the ticket into the 'In Review' TaskState");
    }

    [Fact]
    public async Task Pull_request_closed_with_merged_true_moves_ticket_to_closed_state()
    {
        var projectCode = $"M{DateTime.UtcNow.Ticks % 100000}";
        var repoFullName = $"softim/{projectCode.ToLowerInvariant()}";
        Guid projectId, ticketId;
        int ticketNumber;
        Guid closedStateId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await EnsureLookupsAsync(db);
            var priorityId = await db.TicketPriorities.Where(tp => tp.IsActive).Select(tp => tp.Id).FirstAsync();

            closedStateId = await db.TaskStates
                .Where(ts => ts.IsActive && ts.IsClosedState)
                .Select(ts => ts.Id)
                .FirstOrDefaultAsync();
            if (closedStateId == Guid.Empty)
            {
                closedStateId = Guid.NewGuid();
                db.TaskStates.Add(new TaskState
                {
                    Id = closedStateId,
                    Name = "Done",
                    IsActive = true,
                    IsClosedState = true,
                    SortOrder = 99,
                    CreatedAt = DateTime.UtcNow,
                });
            }
            var defaultStateId = await db.TaskStates.Where(ts => ts.IsActive && !ts.IsClosedState).Select(ts => ts.Id).FirstAsync();

            var project = new Project
            {
                Id = Guid.NewGuid(),
                Code = projectCode,
                Name = $"Proj {projectCode}",
                Status = ProjectStatus.Active,
                ExternalSystem = "GitHub",
                ExternalProjectId = repoFullName,
                CreatedAt = DateTime.UtcNow,
            };
            db.Projects.Add(project);
            var ticket = new Ticket
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                Number = 7,
                Title = "Merge test",
                TicketPriorityId = priorityId,
                TaskStateId = defaultStateId,
                ReporterId = TestDataSeeder.AdminId,
                Position = 0,
                CreatedAt = DateTime.UtcNow,
            };
            db.Tickets.Add(ticket);
            await db.SaveChangesAsync();
            projectId = project.Id;
            ticketId = ticket.Id;
            ticketNumber = ticket.Number;
        }

        var payload = $$"""
            {
              "action": "closed",
              "repository": { "full_name": "{{repoFullName}}" },
              "pull_request": {
                "number": 77,
                "title": "Merge for {{projectCode}}-{{ticketNumber}}",
                "html_url": "https://github.com/{{repoFullName}}/pull/77",
                "head": { "ref": "feat/{{projectCode}}-{{ticketNumber}}-merge" },
                "body": null,
                "merged": true,
                "user": { "login": "bob" },
                "created_at": "2026-04-24T09:00:00Z",
                "closed_at": "2026-04-24T10:05:00Z",
                "merged_at": "2026-04-24T10:05:00Z"
              }
            }
            """;

        using var client = Client();
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/github")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        req.Headers.Add("X-GitHub-Event", "pull_request");
        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var linked = await verifyDb.LinkedPullRequests
            .FirstOrDefaultAsync(lp => lp.TicketId == ticketId && lp.ExternalId == "77");
        linked!.State.Should().Be(PullRequestState.Merged);
        linked.MergedAt.Should().NotBeNull();

        var ticketAfter = await verifyDb.Tickets.FindAsync(ticketId);
        ticketAfter!.TaskStateId.Should().Be(closedStateId);
    }
}
