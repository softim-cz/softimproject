using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Tickets.UpdateTicket;

public sealed record UpdateTicketCommand(
    Guid ProjectId,
    Guid TicketId,
    string Title,
    string? Description,
    Guid TicketPriorityId,
    Guid TaskStateId,
    Guid? AssigneeId,
    DateOnly? DueDate,
    decimal? EstimatedHours,
    Guid? TaskTypeId = null,
    Guid? ParentTicketId = null,
    decimal? ExternalBudget = null,
    string? ExternalUser = null,
    string? ExternalId = null,
    string? ExternalUrl = null,
    string? ExternalProject = null,
    string? ImplementationNotes = null) : IRequest, IRequireProjectRole
{
    public ProjectRole RequiredProjectRole => ProjectRole.Developer;
}

public sealed class UpdateTicketCommandValidator : AbstractValidator<UpdateTicketCommand>
{
    public UpdateTicketCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Description).MaximumLength(10000);
        RuleFor(x => x.TicketPriorityId).NotEmpty();
        RuleFor(x => x.TaskStateId).NotEmpty();
        RuleFor(x => x.EstimatedHours).GreaterThan(0).When(x => x.EstimatedHours.HasValue);
        RuleFor(x => x.ExternalUser).MaximumLength(200);
        RuleFor(x => x.ExternalId).MaximumLength(200);
        RuleFor(x => x.ExternalUrl).MaximumLength(2000);
        RuleFor(x => x.ExternalProject).MaximumLength(256);
        RuleFor(x => x.ImplementationNotes).MaximumLength(10000);
    }
}

public sealed class UpdateTicketCommandHandler(
    IApplicationDbContext dbContext) : IRequestHandler<UpdateTicketCommand>
{
    public async Task Handle(UpdateTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await dbContext.Tickets
            .FirstOrDefaultAsync(t => t.Id == request.TicketId && t.ProjectId == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Ticket), request.TicketId);

        // Reject TaskTypes the project (or its template) does not allow.
        await AllowedTaskTypeResolver.ValidateTaskTypeAsync(dbContext, request.ProjectId, request.TaskTypeId, cancellationToken);

        var taskStateChanged = ticket.TaskStateId != request.TaskStateId;
        var parentChanged = ticket.ParentTicketId != request.ParentTicketId;
        var oldParentId = ticket.ParentTicketId;

        if (parentChanged && request.ParentTicketId.HasValue)
        {
            await ValidateParentAsync(ticket, request.ParentTicketId.Value, cancellationToken);
        }

        ticket.Title = request.Title;
        ticket.Description = request.Description;
        ticket.TicketPriorityId = request.TicketPriorityId;
        ticket.TaskStateId = request.TaskStateId;
        ticket.AssigneeId = request.AssigneeId;
        ticket.DueDate = request.DueDate;
        ticket.EstimatedHours = request.EstimatedHours;
        ticket.TaskTypeId = request.TaskTypeId;
        ticket.ParentTicketId = request.ParentTicketId;
        ticket.ExternalBudget = request.ExternalBudget;
        ticket.ExternalUser = request.ExternalUser;
        ticket.ExternalId = request.ExternalId;
        ticket.ExternalUrl = request.ExternalUrl;
        ticket.ExternalProject = request.ExternalProject;
        ticket.ImplementationNotes = request.ImplementationNotes;

        if (taskStateChanged)
        {
            await SyncColumnForStateAsync(ticket, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (parentChanged)
        {
            // Re-parenting moves the ticket's whole subtree between branches, so both the
            // old and the new ancestor chains need their cumulative totals recomputed.
            await CumulativeWorkedHoursCalculator.RecalculateUpwardAsync(dbContext, oldParentId, cancellationToken);
            await CumulativeWorkedHoursCalculator.RecalculateUpwardAsync(dbContext, request.ParentTicketId, cancellationToken);
        }
    }

    private async Task ValidateParentAsync(Domain.Entities.Ticket ticket, Guid parentId, CancellationToken cancellationToken)
    {
        if (parentId == ticket.Id)
        {
            throw new ValidationException("Ticket nemůže být svým vlastním nadřízeným úkolem.");
        }

        var parent = await dbContext.Tickets
            .Where(t => t.Id == parentId)
            .Select(t => new { t.Id, t.ProjectId, t.ParentTicketId })
            .FirstOrDefaultAsync(cancellationToken);

        if (parent is null)
        {
            throw new ValidationException("Nadřízený úkol nebyl nalezen.");
        }

        if (parent.ProjectId != ticket.ProjectId)
        {
            throw new ValidationException("Nadřízený úkol musí patřit do stejného projektu.");
        }

        // Walk up the parent chain to detect cycles.
        var visited = new HashSet<Guid> { ticket.Id, parent.Id };
        var current = parent.ParentTicketId;
        while (current.HasValue)
        {
            if (!visited.Add(current.Value))
            {
                throw new ValidationException("Vazba na nadřízený úkol by vytvořila cyklus.");
            }

            var next = await dbContext.Tickets
                .Where(t => t.Id == current.Value)
                .Select(t => t.ParentTicketId)
                .FirstOrDefaultAsync(cancellationToken);

            current = next;
        }
    }

    private async Task SyncColumnForStateAsync(Domain.Entities.Ticket ticket, CancellationToken cancellationToken)
    {
        // Keep current column if it already maps the new state; otherwise pick the first column on the
        // default board that maps it. Without this, sidebar status edits would leave tickets stranded
        // in their original kanban column.
        if (ticket.ColumnId.HasValue)
        {
            var currentColumn = await dbContext.KanbanColumns
                .Include(c => c.MapsToTaskStates)
                .FirstOrDefaultAsync(c => c.Id == ticket.ColumnId.Value, cancellationToken);

            if (currentColumn is not null && currentColumn.MapsToTaskStates.Any(ts => ts.Id == ticket.TaskStateId))
            {
                return;
            }
        }

        var targetColumn = await dbContext.KanbanColumns
            .Where(c => c.Board.ProjectId == ticket.ProjectId
                        && c.Board.IsDefault
                        && c.MapsToTaskStates.Any(ts => ts.Id == ticket.TaskStateId))
            .OrderBy(c => c.Position)
            .Select(c => new { c.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (targetColumn is not null)
        {
            ticket.ColumnId = targetColumn.Id;
        }
    }
}
