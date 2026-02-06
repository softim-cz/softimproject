using FluentValidation;
using MediatR;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Tickets.CreateTicket;

public sealed record CreateTicketCommand(
    Guid ProjectId,
    string Title,
    string? Description,
    TicketPriority Priority,
    Guid? AssigneeId,
    Guid? ColumnId,
    DateOnly? DueDate,
    decimal? EstimatedHours) : IRequest<Guid>, IRequireProjectAccess;

public sealed class CreateTicketCommandValidator : AbstractValidator<CreateTicketCommand>
{
    public CreateTicketCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Description).MaximumLength(10000);
        RuleFor(x => x.Priority).IsInEnum();
        RuleFor(x => x.EstimatedHours).GreaterThan(0).When(x => x.EstimatedHours.HasValue);
    }
}

public sealed class CreateTicketCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService) : IRequestHandler<CreateTicketCommand, Guid>
{
    public async Task<Guid> Handle(CreateTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority,
            Status = TicketStatus.Backlog,
            AssigneeId = request.AssigneeId,
            ColumnId = request.ColumnId,
            ReporterId = currentUserService.UserId ?? Guid.Empty,
            DueDate = request.DueDate,
            EstimatedHours = request.EstimatedHours,
            Position = 0,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ticket.Id;
    }
}
