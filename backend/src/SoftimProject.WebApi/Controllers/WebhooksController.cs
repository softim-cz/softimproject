using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

namespace SoftimProject.WebApi.Controllers;

// Thin transport layer for GitHub webhooks (#111). Responsibilities here:
//   1. signature verification (HMAC over the raw body using the project secret)
//   2. delivery-id idempotency (skip a redelivered X-GitHub-Delivery)
//   3. hand the payload to IGitHubWebhookProcessor; on failure dead-letter it for replay
// All domain mutation lives in the processor so the same code path serves DLQ replay.
[Route("api/webhooks")]
[ApiController]
[AllowAnonymous]
public class WebhooksController(
    IApplicationDbContext dbContext,
    IGitHubWebhookProcessor processor,
    IDeadLetterQueue deadLetters) : ControllerBase
{
    [HttpPost("github")]
    public async Task<IActionResult> GitHubWebhook(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(ct);

        var eventType = Request.Headers["X-GitHub-Event"].FirstOrDefault();
        if (string.IsNullOrEmpty(eventType))
            return BadRequest("Missing X-GitHub-Event header");

        var deliveryId = Request.Headers["X-GitHub-Delivery"].FirstOrDefault();

        // Parse the payload just far enough to find the project + verify the signature.
        string? repoFullName;
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (!root.TryGetProperty("repository", out var repoEl) || !repoEl.TryGetProperty("full_name", out var fullNameEl))
                return BadRequest("Missing repository info");
            repoFullName = fullNameEl.GetString();
        }
        catch (JsonException)
        {
            return BadRequest("Invalid JSON payload");
        }

        var project = await dbContext.Projects
            .FirstOrDefaultAsync(p => p.ExternalSystem == "GitHub" && p.ExternalProjectId == repoFullName, ct);
        if (project == null)
            return NotFound("No project configured for this repository");

        if (!string.IsNullOrEmpty(project.WebhookSecret))
        {
            var signature = Request.Headers["X-Hub-Signature-256"].FirstOrDefault();
            if (!VerifySignature(body, signature, project.WebhookSecret))
                return Unauthorized("Invalid webhook signature");
        }

        // Idempotency: a delivery id we've already accepted is a no-op. Protects against
        // GitHub's automatic redelivery and our own DLQ replay double-applying an event.
        if (!string.IsNullOrEmpty(deliveryId))
        {
            var already = await dbContext.ProcessedWebhookDeliveries
                .AnyAsync(d => d.Provider == "GitHub" && d.DeliveryId == deliveryId, ct);
            if (already)
                return Ok(new { message = "Duplicate delivery ignored" });
        }

        try
        {
            var result = await processor.ProcessAsync(eventType, body, ct);
            await RecordDeliveryAsync(deliveryId, eventType, ct);
            return Ok(new { message = result.Message });
        }
        catch (Exception ex)
        {
            // Don't lose the event: park it in the DLQ keyed by delivery id for admin replay,
            // and record the delivery so a GitHub auto-retry doesn't pile on duplicates.
            var payload = JsonSerializer.Serialize(new GitHubWebhookPayload(eventType, body));
            await deadLetters.EnqueueAsync(
                DeadLetterOperation.GitHubWebhook,
                string.IsNullOrEmpty(deliveryId) ? $"{eventType}:{Guid.NewGuid()}" : deliveryId,
                payload,
                ex,
                ct);
            await RecordDeliveryAsync(deliveryId, eventType, ct);
            return Accepted(new { message = "Processing failed; queued for retry" });
        }
    }

    private async Task RecordDeliveryAsync(string? deliveryId, string eventType, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(deliveryId)) return;
        dbContext.ProcessedWebhookDeliveries.Add(new ProcessedWebhookDelivery
        {
            Id = Guid.NewGuid(),
            Provider = "GitHub",
            DeliveryId = deliveryId,
            EventType = eventType,
            ReceivedAt = DateTime.UtcNow,
        });
        await dbContext.SaveChangesAsync(ct);
    }

    private static bool VerifySignature(string payload, string? signature, string secret)
    {
        if (string.IsNullOrEmpty(signature)) return false;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var expected = "sha256=" + Convert.ToHexStringLower(hash);
        return string.Equals(expected, signature, StringComparison.OrdinalIgnoreCase);
    }

    // Shape stored in the DLQ payload so a replay can reconstruct the original delivery.
    public sealed record GitHubWebhookPayload(string EventType, string Body);
}
