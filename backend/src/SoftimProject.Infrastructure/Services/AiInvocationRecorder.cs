using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Infrastructure.Services;

public sealed class AiInvocationRecorder(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<AiInvocationRecorder> logger) : IAiInvocationRecorder
{
    // Simple per-user token-bucket-ish check: count invocations the user has made
    // in the last `windowMinutes` minutes; reject if it crosses `limit`. Keeps the
    // state in SQL so background jobs and API calls see the same running total.
    private int RateLimitCalls => configuration.GetValue("Ai:RateLimit:CallsPerWindow", 20);
    private int RateLimitWindowMinutes => configuration.GetValue("Ai:RateLimit:WindowMinutes", 10);

    private AiPricing Pricing => new()
    {
        Model = configuration.GetValue("Ai:Pricing:Model", "gpt-4o")!,
        InputPerMillionTokensUsd = configuration.GetValue("Ai:Pricing:InputPerMillionTokensUsd", 2.50m),
        OutputPerMillionTokensUsd = configuration.GetValue("Ai:Pricing:OutputPerMillionTokensUsd", 10.00m),
    };

    public async Task<AiInvocationResult<T>> RecordAsync<T>(
        AiInvocationContext context,
        Func<CancellationToken, Task<AiInvocationCall<T>>> call,
        CancellationToken cancellationToken = default)
    {
        // Rate-limit guard applies only when a user scope is present — background
        // jobs (no user) are governed by their own schedule, not the per-user cap.
        if (context.TriggeredByUserId.HasValue)
        {
            using var limitScope = scopeFactory.CreateScope();
            var limitDb = limitScope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var since = DateTime.UtcNow.AddMinutes(-RateLimitWindowMinutes);
            var recent = await limitDb.AiInvocations
                .CountAsync(
                    x => x.TriggeredByUserId == context.TriggeredByUserId && x.StartedAt >= since,
                    cancellationToken);
            if (recent >= RateLimitCalls)
            {
                throw new AiRateLimitExceededException(
                    $"AI rate limit exceeded: {RateLimitCalls} calls per {RateLimitWindowMinutes} minutes per user.");
            }
        }

        var id = Guid.NewGuid();
        var startedAt = DateTime.UtcNow;
        var inputHash = ComputeHash(context.InputText);
        var pricing = Pricing;
        var sw = Stopwatch.StartNew();

        AiInvocationCall<T>? result = null;
        Exception? failure = null;
        try
        {
            result = await call(cancellationToken);
        }
        catch (Exception ex)
        {
            failure = ex;
        }

        sw.Stop();
        var endedAt = DateTime.UtcNow;

        using var writeScope = scopeFactory.CreateScope();
        var db = writeScope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        db.AiInvocations.Add(new AiInvocation
        {
            Id = id,
            Trigger = context.Trigger,
            TriggeredByUserId = context.TriggeredByUserId,
            ProjectId = context.ProjectId,
            TicketId = context.TicketId,
            InputHash = inputHash,
            Model = pricing.Model,
            PromptTokens = result?.PromptTokens ?? 0,
            CompletionTokens = result?.CompletionTokens ?? 0,
            TotalTokens = (result?.PromptTokens ?? 0) + (result?.CompletionTokens ?? 0),
            EstimatedCostUsd = pricing.EstimateCostUsd(
                result?.PromptTokens ?? 0,
                result?.CompletionTokens ?? 0),
            OutputPreview = Truncate(result?.OutputPreview, 1000),
            Success = failure is null,
            ErrorMessage = Truncate(failure?.Message, 4000),
            StartedAt = startedAt,
            CompletedAt = endedAt,
            DurationMs = sw.ElapsedMilliseconds,
            Reason = context.Reason,
        });
        await db.SaveChangesAsync(cancellationToken);

        if (failure is not null)
        {
            logger.LogWarning(failure, "AI invocation {InvocationId} failed ({Trigger})", id, context.Trigger);
            throw failure;
        }

        return new AiInvocationResult<T>(result!.Payload, id);
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input ?? string.Empty));
        return Convert.ToHexStringLower(bytes);
    }

    private static string? Truncate(string? value, int max)
        => string.IsNullOrEmpty(value) || value.Length <= max ? value : value[..max];
}
