using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Worklogs.CreateWorklogsBatch;

/// <summary>
/// Records several worklog entries against a single ticket in one transaction
/// (the "hromadný worklog" flow, #53). All items share the ticket; each carries its
/// own date, hours and description. The budget check ("Předepsaná doba") is advisory and
/// lives on the client — the server never blocks an over-budget entry.
/// </summary>
public sealed record CreateWorklogsBatchCommand(
    Guid ProjectId,
    Guid TicketId,
    IReadOnlyList<CreateWorklogsBatchItem> Items,
    Guid? OverrideUserId = null) : IRequest<IReadOnlyList<Guid>>, IRequireProjectRole
{
    public ProjectRole RequiredProjectRole => ProjectRole.Developer;
}

public sealed record CreateWorklogsBatchItem(
    DateOnly Date,
    decimal Hours,
    string Description,
    bool IsBillable);

public sealed class CreateWorklogsBatchCommandValidator : AbstractValidator<CreateWorklogsBatchCommand>
{
    public CreateWorklogsBatchCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty();
        // Guard against a runaway payload; the bulk UI tops out well below this.
        RuleFor(x => x.Items).Must(items => items.Count <= 50)
            .WithMessage("A batch may contain at most 50 worklog entries.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Hours).GreaterThan(0).LessThanOrEqualTo(24);
            item.RuleFor(i => i.Date).NotEmpty();
            item.RuleFor(i => i.Description)
                .NotEmpty()
                .MinimumLength(16)
                .MaximumLength(2000);
        });
    }
}

public sealed class CreateWorklogsBatchCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService) : IRequestHandler<CreateWorklogsBatchCommand, IReadOnlyList<Guid>>
{
    public async Task<IReadOnlyList<Guid>> Handle(CreateWorklogsBatchCommand request, CancellationToken cancellationToken)
    {
        await dbContext.GetTicketForProjectAsync(request.ProjectId, request.TicketId, cancellationToken);

        var callerId = currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Current user is not initialized.");

        var ownerId = callerId;
        if (request.OverrideUserId.HasValue && request.OverrideUserId.Value != callerId)
        {
            if (!currentUserService.IsInRole("Admin"))
                throw new UnauthorizedAccessException("Only Admin can record worklogs on behalf of another user.");

            var exists = await dbContext.Users.AnyAsync(u => u.Id == request.OverrideUserId.Value, cancellationToken);
            if (!exists)
                throw new NotFoundException(nameof(User), request.OverrideUserId.Value);

            ownerId = request.OverrideUserId.Value;
        }

        var now = DateTime.UtcNow;
        var ids = new List<Guid>(request.Items.Count);
        foreach (var item in request.Items)
        {
            var worklog = new Worklog
            {
                Id = Guid.NewGuid(),
                TicketId = request.TicketId,
                UserId = ownerId,
                Date = item.Date,
                Hours = item.Hours,
                Description = item.Description,
                Source = WorklogSource.Manual,
                IsBillable = item.IsBillable,
                CreatedAt = now
            };
            dbContext.Worklogs.Add(worklog);
            ids.Add(worklog.Id);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // One recalculation covers all inserts since they share the ticket.
        await CumulativeWorkedHoursCalculator.RecalculateUpwardAsync(dbContext, request.TicketId, cancellationToken);

        return ids;
    }
}
