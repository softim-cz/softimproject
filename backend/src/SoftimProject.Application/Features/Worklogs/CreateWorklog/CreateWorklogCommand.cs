using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Worklogs.CreateWorklog;

public sealed record CreateWorklogCommand(
    Guid ProjectId,
    Guid TicketId,
    DateOnly Date,
    decimal Hours,
    string Description,
    bool IsBillable,
    Guid? OverrideUserId = null) : IRequest<Guid>, IRequireProjectRole
{
    public ProjectRole RequiredProjectRole => ProjectRole.Developer;
}

public sealed class CreateWorklogCommandValidator : AbstractValidator<CreateWorklogCommand>
{
    public CreateWorklogCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.Hours).GreaterThan(0).LessThanOrEqualTo(24);
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.Description)
            .NotEmpty()
            .MinimumLength(16)
            .MaximumLength(2000);
    }
}

public sealed class CreateWorklogCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService) : IRequestHandler<CreateWorklogCommand, Guid>
{
    public async Task<Guid> Handle(CreateWorklogCommand request, CancellationToken cancellationToken)
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

        var worklog = new Worklog
        {
            Id = Guid.NewGuid(),
            TicketId = request.TicketId,
            UserId = ownerId,
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
