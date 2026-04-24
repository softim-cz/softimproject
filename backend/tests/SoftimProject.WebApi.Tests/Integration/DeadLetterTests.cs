using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SoftimProject.Application.Features.Admin;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;
using SoftimProject.Infrastructure.Persistence;

namespace SoftimProject.WebApi.Tests.Integration;

// Covers the #13 DLQ surface:
//   - EnqueueAsync upsert semantics: repeated failures for the same key increment the
//     attempt counter instead of creating rows
//   - Admin-only listing (non-admin → 403)
//   - Dismiss flips Pending → Dismissed
//   - Replay routes through IDeadLetterReplayer: supported operation types succeed,
//     unsupported ones surface 400 with "not supported" message
[Collection("Integration")]
public sealed class DeadLetterTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public DeadLetterTests(IntegrationTestFactory factory) => _factory = factory;

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

    [Fact]
    public async Task Enqueue_upserts_on_duplicate_operation_key_instead_of_inserting_new_rows()
    {
        using var scope = _factory.Services.CreateScope();
        var queue = scope.ServiceProvider.GetRequiredService<IDeadLetterQueue>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var key = $"ticket-{Guid.NewGuid()}";

        await queue.EnqueueAsync(DeadLetterOperation.AiSummarizeTicket, key, "{}", new InvalidOperationException("boom 1"));
        await queue.EnqueueAsync(DeadLetterOperation.AiSummarizeTicket, key, "{}", new InvalidOperationException("boom 2"));
        await queue.EnqueueAsync(DeadLetterOperation.AiSummarizeTicket, key, "{}", new InvalidOperationException("boom 3"));

        db.ChangeTracker.Clear();
        var rows = db.DeadLetterEntries
            .Where(e => e.OperationType == DeadLetterOperation.AiSummarizeTicket && e.OperationKey == key)
            .ToList();
        rows.Should().HaveCount(1);
        rows[0].AttemptCount.Should().Be(3);
        rows[0].LastError.Should().Contain("boom 3");
        rows[0].Status.Should().Be(DeadLetterStatus.Pending);
    }

    [Fact]
    public async Task Non_admin_is_forbidden_from_listing_dead_letter()
    {
        using var client = ClientAs(TestDataSeeder.UserAOid);
        var response = await client.GetAsync("/api/v1/admin/dead-letter");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_list_shows_pending_entries_and_dismiss_moves_them_out()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var entry = new DeadLetterEntry
        {
            Id = Guid.NewGuid(),
            OperationType = DeadLetterOperation.GitHubSyncProject,
            OperationKey = $"project-{Guid.NewGuid()}",
            Payload = "{}",
            AttemptCount = 3,
            LastError = "simulated 502",
            FirstFailedAt = DateTime.UtcNow.AddMinutes(-10),
            LastFailedAt = DateTime.UtcNow.AddMinutes(-1),
            Status = DeadLetterStatus.Pending,
        };
        db.DeadLetterEntries.Add(entry);
        await db.SaveChangesAsync();

        using var client = ClientAs(TestDataSeeder.AdminOid);
        var list = await client.GetFromJsonAsync<List<DeadLetterEntryDto>>(
            "/api/v1/admin/dead-letter", JsonOptions);
        list.Should().Contain(x => x.Id == entry.Id);

        var dismissed = await client.PostAsync($"/api/v1/admin/dead-letter/{entry.Id}/dismiss", content: null);
        dismissed.StatusCode.Should().Be(HttpStatusCode.NoContent);

        db.ChangeTracker.Clear();
        var reloaded = await db.DeadLetterEntries.FindAsync(entry.Id);
        reloaded!.Status.Should().Be(DeadLetterStatus.Dismissed);
        reloaded.ResolvedAt.Should().NotBeNull();

        // Default listing hides non-Pending entries.
        var afterList = await client.GetFromJsonAsync<List<DeadLetterEntryDto>>(
            "/api/v1/admin/dead-letter", JsonOptions);
        afterList.Should().NotContain(x => x.Id == entry.Id);
    }

    [Fact]
    public async Task Replay_returns_400_for_operation_types_without_a_handler()
    {
        // GitHubWebhook isn't wired to a replay handler — endpoint must refuse clearly.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var entry = new DeadLetterEntry
        {
            Id = Guid.NewGuid(),
            OperationType = DeadLetterOperation.GitHubWebhook,
            OperationKey = $"delivery-{Guid.NewGuid()}",
            Payload = "{}",
            AttemptCount = 1,
            LastError = "",
            FirstFailedAt = DateTime.UtcNow,
            LastFailedAt = DateTime.UtcNow,
            Status = DeadLetterStatus.Pending,
        };
        db.DeadLetterEntries.Add(entry);
        await db.SaveChangesAsync();

        using var client = ClientAs(TestDataSeeder.AdminOid);
        var response = await client.PostAsync($"/api/v1/admin/dead-letter/{entry.Id}/replay", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("not supported", because: "the replayer has no handler registered for GitHubWebhook yet");
    }
}
