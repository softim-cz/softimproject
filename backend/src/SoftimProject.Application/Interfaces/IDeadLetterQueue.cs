using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Interfaces;

// Terminal sink for operations that exhausted their retry budget. EnqueueAsync is
// upsert-by-key: the same unit failing repeatedly accumulates on a single row with
// AttemptCount incremented, so the admin backlog doesn't balloon. Replay is handled
// by IDeadLetterReplayer, keyed off OperationType.
public interface IDeadLetterQueue
{
    Task EnqueueAsync(
        DeadLetterOperation operationType,
        string operationKey,
        string payload,
        Exception exception,
        CancellationToken cancellationToken = default);
}
