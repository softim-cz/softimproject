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
    TicketPriority Priority,
    TicketStatus Status,
    Guid? AssigneeId,
    DateOnly? DueDate,
    decimal? EstimatedHours) : IRequest, IRequireProjectAccess;

public sealed class UpdateTicketCommandValidator : AbstractValidator<UpdateTicketCommand>
{
    public UpdateTicketCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Description).MaximumLength(10000);
        RuleFor(x => x.Priority).IsInEnum();
        RuleFor(x => x.Status).IsInEnum();
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
        ticket.Priority = request.Priority;
        ticket.Status = request.Status;
        ticket.AssigneeId = request.AssigneeId;
        ticket.DueDate = request.DueDate;
        ticket.EstimatedHours = request.EstimatedHours;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
