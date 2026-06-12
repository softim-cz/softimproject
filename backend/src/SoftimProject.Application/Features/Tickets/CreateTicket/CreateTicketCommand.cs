using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Application.Features.Tickets.CreateTicket;

public sealed record CreateTicketCommand(
    Guid ProjectId,
    string Title,
    string? Description,
    Guid TicketPriorityId,
    Guid? AssigneeId,
    Guid? ColumnId,
    DateOnly? DueDate,
    decimal? EstimatedHours,
    Guid? TaskTypeId = null,
    Guid? TaskStateId = null,
    Guid? ParentTicketId = null,
    decimal? ExternalBudget = null,
    string? ExternalUser = null,
    string? ExternalProject = null) : IRequest<Guid>, IRequireProjectAccess;

public sealed class CreateTicketCommandValidator : AbstractValidator<CreateTicketCommand>
{
    public CreateTicketCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Description).MaximumLength(10000);
        RuleFor(x => x.TicketPriorityId).NotEmpty();
        RuleFor(x => x.EstimatedHours).GreaterThan(0).When(x => x.EstimatedHours.HasValue);
    }
}

public sealed class CreateTicketCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService) : IRequestHandler<CreateTicketCommand, Guid>
{
    public async Task<Guid> Handle(CreateTicketCommand request, CancellationToken cancellationToken)
    {
        // Load project to assign next ticket number
        var project = await dbContext.Projects.FindAsync([request.ProjectId], cancellationToken)
            ?? throw new Common.NotFoundException(nameof(Domain.Entities.Project), request.ProjectId);

        // Reject TaskTypes the project (or its template) does not allow.
        await AllowedTaskTypeResolver.ValidateTaskTypeAsync(dbContext, request.ProjectId, request.TaskTypeId, cancellationToken);

        // Resolve TaskStateId: use provided or find default scoped to project's template
        var taskStateId = request.TaskStateId;
        if (!taskStateId.HasValue)
        {
            var templateId = project.ProjectTemplateId;
            var query = dbContext.TaskStates
                .Where(ts => ts.IsActive && ts.ProjectTemplateId == templateId);

            taskStateId = await query
                .Where(ts => ts.IsDefault)
                .Select(ts => ts.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (taskStateId == Guid.Empty)
            {
                taskStateId = await query
                    .OrderBy(ts => ts.SortOrder)
                    .Select(ts => ts.Id)
                    .FirstAsync(cancellationToken);
            }
        }

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            Number = project.NextTicketNumber++,
            Title = request.Title,
            Description = request.Description,
            TicketPriorityId = request.TicketPriorityId,
            TaskStateId = taskStateId.Value,
            AssigneeId = request.AssigneeId,
            ColumnId = request.ColumnId,
            ReporterId = currentUserService.UserId ?? Guid.Empty,
            DueDate = request.DueDate,
            EstimatedHours = request.EstimatedHours,
            TaskTypeId = request.TaskTypeId,
            ParentTicketId = request.ParentTicketId,
            ExternalBudget = request.ExternalBudget,
            ExternalUser = request.ExternalUser,
            ExternalProject = request.ExternalProject,
            Position = 0,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ticket.Id;
    }
}
