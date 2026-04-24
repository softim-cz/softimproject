using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Interfaces;

// Central dispatcher invoked by the admin Replay endpoint. Looks up a handler
// registered for the entry's OperationType and delegates. Operations without a
// handler are "list-only / dismiss" — replay returns false.
public interface IDeadLetterReplayer
{
    Task<ReplayOutcome> ReplayAsync(DeadLetterEntry entry, CancellationToken cancellationToken = default);
}

public interface IDeadLetterReplayHandler
{
    DeadLetterOperation OperationType { get; }
    Task<ReplayOutcome> ReplayAsync(DeadLetterEntry entry, CancellationToken cancellationToken = default);
}

public sealed record ReplayOutcome(bool Succeeded, string? ErrorMessage = null);
