using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Infrastructure.Services;

public sealed class DeadLetterQueue(
    IServiceScopeFactory scopeFactory,
    ILogger<DeadLetterQueue> logger) : IDeadLetterQueue
{
    public async Task EnqueueAsync(
        DeadLetterOperation operationType,
        string operationKey,
        string payload,
        Exception exception,
        CancellationToken cancellationToken = default)
    {
        // Callers are typically background services catching mid-iteration; use our own
        // scope so we don't depend on the caller's DbContext lifetime.
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var existing = await db.DeadLetterEntries
            .FirstOrDefaultAsync(
                e => e.OperationType == operationType && e.OperationKey == operationKey,
                cancellationToken);

        var now = DateTime.UtcNow;
        // Truncate error to the column max so a huge stacktrace doesn't take the save down.
        var lastError = Truncate(exception.ToString(), 4000);

        if (existing is null)
        {
            db.DeadLetterEntries.Add(new DeadLetterEntry
            {
                Id = Guid.NewGuid(),
                OperationType = operationType,
                OperationKey = operationKey,
                Payload = payload,
                AttemptCount = 1,
                LastError = lastError,
                FirstFailedAt = now,
                LastFailedAt = now,
                Status = DeadLetterStatus.Pending,
            });
        }
        else
        {
            existing.AttemptCount++;
            existing.LastError = lastError;
            existing.LastFailedAt = now;
            existing.Payload = payload; // Keep the latest payload (often enriched over retries).
            existing.Status = DeadLetterStatus.Pending;
            existing.ResolvedAt = null;
            existing.ResolvedByUserId = null;
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogError(exception,
            "Dead-lettered {OperationType} key={OperationKey} attempt={Attempt}",
            operationType, operationKey, (existing?.AttemptCount ?? 1));
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];
}
