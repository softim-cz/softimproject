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
    string? ExternalUser = null) : IRequest, IRequireProjectRole
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

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
