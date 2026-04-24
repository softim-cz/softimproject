using Microsoft.Extensions.Logging;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Infrastructure.Services;

public sealed class DeadLetterReplayer(
    IEnumerable<IDeadLetterReplayHandler> handlers,
    ILogger<DeadLetterReplayer> logger) : IDeadLetterReplayer
{
    private readonly Dictionary<Domain.Enums.DeadLetterOperation, IDeadLetterReplayHandler> _handlers
        = handlers.ToDictionary(h => h.OperationType);

    public async Task<ReplayOutcome> ReplayAsync(DeadLetterEntry entry, CancellationToken cancellationToken = default)
    {
        if (!_handlers.TryGetValue(entry.OperationType, out var handler))
        {
            var message = $"Replay is not supported for operation type {entry.OperationType}. Dismiss the entry instead.";
            logger.LogWarning(
                "Replay rejected: no handler for {OperationType} (entry {EntryId})",
                entry.OperationType, entry.Id);
            return new ReplayOutcome(false, message);
        }

        try
        {
            return await handler.ReplayAsync(entry, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Replay handler {Handler} threw for entry {EntryId}",
                handler.GetType().Name, entry.Id);
            return new ReplayOutcome(false, ex.Message);
        }
    }
}
