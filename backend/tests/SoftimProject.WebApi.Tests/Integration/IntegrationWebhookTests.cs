using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;
using SoftimProject.Infrastructure.Persistence;

namespace SoftimProject.WebApi.Tests.Integration;

// #144 M5 — generic integration webhook: signature verification, delivery-id idempotency,
// 202 trigger. The seeded connection intentionally has no token so the background sync
// hard-fails fast (no network) — these tests cover the transport layer, not the sync.
[Collection("Integration")]
public sealed class IntegrationWebhookTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;
    public IntegrationWebhookTests(IntegrationTestFactory factory) => _factory = factory;

    private HttpClient Client()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    private const string Secret = "webhook-secret-123";

    private async Task<Guid> SeedConnectionAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var connection = new IntegrationConnection
        {
            Id = Guid.NewGuid(),
            Name = "Test EP",
            SourceSystem = SyncType.EasyProject,
            BaseUrl = "https://ep.example",
            WebhookSecret = Secret,
            EncryptedApiToken = null, // → runner hard-fails fast, no network
            TargetProjectTemplateId = Guid.NewGuid(),
            CreatedByUserId = TestDataSeeder.AdminId,
            Mode = IntegrationSyncMode.IncrementalOnly,
            IsEnabled = true,
            IntervalMinutes = 60,
            CreatedAt = DateTime.UtcNow,
        };
        db.IntegrationConnections.Add(connection);
        await db.SaveChangesAsync();
        return connection.Id;
    }

    private static string Sign(string body)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Secret));
        return "sha256=" + Convert.ToHexStringLower(hmac.ComputeHash(Encoding.UTF8.GetBytes(body)));
    }

    private static HttpRequestMessage Request(Guid connectionId, string body, string? signature, string? deliveryId = null)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, $"/api/webhooks/integration/{connectionId}")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        if (signature != null) req.Headers.Add("X-Signature-256", signature);
        if (deliveryId != null) req.Headers.Add("X-Delivery-Id", deliveryId);
        return req;
    }

    [Fact]
    public async Task Unknown_connection_returns_404()
    {
        using var client = Client();
        var response = await client.SendAsync(Request(Guid.NewGuid(), "{}", Sign("{}")));
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Invalid_signature_returns_401()
    {
        var connectionId = await SeedConnectionAsync();
        using var client = Client();
        var response = await client.SendAsync(Request(connectionId, "{\"x\":1}", "sha256=deadbeef"));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Valid_signature_triggers_sync_and_records_delivery()
    {
        var connectionId = await SeedConnectionAsync();
        const string body = "{\"event\":\"issue.updated\"}";
        var deliveryId = $"dlv-{Guid.NewGuid()}";

        using var client = Client();
        var response = await client.SendAsync(Request(connectionId, body, Sign(body), deliveryId));
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        (await db.ProcessedWebhookDeliveries.CountAsync(d => d.Provider == "EasyProject" && d.DeliveryId == deliveryId))
            .Should().Be(1);
    }

    [Fact]
    public async Task Duplicate_delivery_is_ignored()
    {
        var connectionId = await SeedConnectionAsync();
        const string body = "{\"event\":\"issue.updated\"}";
        var deliveryId = $"dup-{Guid.NewGuid()}";

        using var client = Client();
        (await client.SendAsync(Request(connectionId, body, Sign(body), deliveryId))).StatusCode
            .Should().Be(HttpStatusCode.Accepted);

        var second = await client.SendAsync(Request(connectionId, body, Sign(body), deliveryId));
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        (await second.Content.ReadAsStringAsync()).Should().Contain("Duplicate");
    }
}
