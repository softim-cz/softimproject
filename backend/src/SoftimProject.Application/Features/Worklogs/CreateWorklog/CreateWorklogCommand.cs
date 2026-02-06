using FluentValidation;
using MediatR;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Worklogs.CreateWorklog;

public sealed record CreateWorklogCommand(
    Guid ProjectId,
    Guid? TicketId,
    DateOnly Date,
    decimal Hours,
    string? Description,
    bool IsBillable) : IRequest<Guid>, IRequireProjectAccess;

public sealed class CreateWorklogCommandValidator : AbstractValidator<CreateWorklogCommand>
{
    public CreateWorklogCommandValidator()
    {
        RuleFor(x => x.Hours).GreaterThan(0).LessThanOrEqualTo(24);
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}

public sealed class CreateWorklogCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService) : IRequestHandler<CreateWorklogCommand, Guid>
{
    public async Task<Guid> Handle(CreateWorklogCommand request, CancellationToken cancellationToken)
    {
        var worklog = new Worklog
        {
            Id = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            TicketId = request.TicketId,
            UserId = currentUserService.UserId ?? Guid.Empty,
            Date = request.Date,
            Hours = request.Hours,
            Description = request.Description,
            Source = WorklogSource.Manual,
            IsBillable = request.IsBillable,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Worklogs.Add(worklog);
        await dbContext.SaveChangesAsync(cancellationToken);

        return worklog.Id;
    }
}
