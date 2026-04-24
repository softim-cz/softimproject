using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Admin;

public sealed record DeadLetterEntryDto(
    Guid Id,
    DeadLetterOperation OperationType,
    string OperationKey,
    string Payload,
    int AttemptCount,
    string LastError,
    DateTime FirstFailedAt,
    DateTime LastFailedAt,
    DeadLetterStatus Status,
    DateTime? ResolvedAt);

// LIST — pending first, then resolved/dismissed; newest failure on top.
public sealed record GetDeadLetterEntriesQuery(bool IncludeResolved = false)
    : IRequest<List<DeadLetterEntryDto>>, IRequireRole
{
    public string RequiredRole => "Admin";
}

public sealed class GetDeadLetterEntriesQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetDeadLetterEntriesQuery, List<DeadLetterEntryDto>>
{
    public async Task<List<DeadLetterEntryDto>> Handle(
        GetDeadLetterEntriesQuery request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.DeadLetterEntries.AsNoTracking().AsQueryable();
        if (!request.IncludeResolved)
            query = query.Where(e => e.Status == DeadLetterStatus.Pending);

        return await query
            .OrderByDescending(e => e.LastFailedAt)
            .Select(e => new DeadLetterEntryDto(
                e.Id,
                e.OperationType,
                e.OperationKey,
                e.Payload,
                e.AttemptCount,
                e.LastError,
                e.FirstFailedAt,
                e.LastFailedAt,
                e.Status,
                e.ResolvedAt))
            .ToListAsync(cancellationToken);
    }
}

// REPLAY — idempotent operation types only; others return "not supported" as part
// of the ValidationException (→ 400). Replay success moves the row to Replayed.
public sealed record ReplayDeadLetterCommand(Guid Id) : IRequest, IRequireRole
{
    public string RequiredRole => "Admin";
}

public sealed class ReplayDeadLetterCommandHandler(
    IApplicationDbContext dbContext,
    IDeadLetterReplayer replayer,
    ICurrentUserService currentUser)
    : IRequestHandler<ReplayDeadLetterCommand>
{
    public async Task Handle(ReplayDeadLetterCommand request, CancellationToken cancellationToken)
    {
        var entry = await dbContext.DeadLetterEntries
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(DeadLetterEntry), request.Id);

        if (entry.Status != DeadLetterStatus.Pending)
            throw new FluentValidation.ValidationException(
                $"Entry is already {entry.Status}; only Pending entries can be replayed.");

        var outcome = await replayer.ReplayAsync(entry, cancellationToken);
        if (!outcome.Succeeded)
            throw new FluentValidation.ValidationException(
                outcome.ErrorMessage ?? "Replay failed without a specific reason.");

        entry.Status = DeadLetterStatus.Replayed;
        entry.ResolvedAt = DateTime.UtcNow;
        entry.ResolvedByUserId = currentUser.UserId?.ToString();
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

// DISMISS — admin explicitly marks the entry as handled outside the app. Keeps
// the row for audit but removes it from the default Pending list.
public sealed record DismissDeadLetterCommand(Guid Id) : IRequest, IRequireRole
{
    public string RequiredRole => "Admin";
}

public sealed class DismissDeadLetterCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUser)
    : IRequestHandler<DismissDeadLetterCommand>
{
    public async Task Handle(DismissDeadLetterCommand request, CancellationToken cancellationToken)
    {
        var entry = await dbContext.DeadLetterEntries
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(DeadLetterEntry), request.Id);

        entry.Status = DeadLetterStatus.Dismissed;
        entry.ResolvedAt = DateTime.UtcNow;
        entry.ResolvedByUserId = currentUser.UserId?.ToString();
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
