using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Worklogs.UpdateWorklog;

public sealed record UpdateWorklogCommand(
    Guid ProjectId,
    Guid WorklogId,
    Guid TicketId,
    DateOnly Date,
    decimal Hours,
    string Description,
    bool IsBillable,
    string? Invoiced,
    Guid? OverrideUserId = null) : IRequest, IRequireProjectRole
{
    public ProjectRole RequiredProjectRole => ProjectRole.Developer;
}

public sealed class UpdateWorklogCommandValidator : AbstractValidator<UpdateWorklogCommand>
{
    public UpdateWorklogCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.Hours).GreaterThan(0).LessThanOrEqualTo(24);
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.Description)
            .NotEmpty()
            .MinimumLength(16)
            .MaximumLength(2000);
        RuleFor(x => x.Invoiced).MaximumLength(200);
    }
}

public sealed class UpdateWorklogCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService) : IRequestHandler<UpdateWorklogCommand>
{
    public async Task Handle(UpdateWorklogCommand request, CancellationToken cancellationToken)
    {
        var worklog = await dbContext.GetWorklogForProjectAsync(request.ProjectId, request.WorklogId, cancellationToken);

        var callerId = currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Current user is not initialized.");

        var isAdmin = currentUserService.IsInRole("Admin");

        if (worklog.UserId != callerId && !isAdmin)
        {
            var isProjectManager = await dbContext.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == request.ProjectId
                    && pm.UserId == callerId
                    && pm.Role == ProjectRole.ProjectManager, cancellationToken);
            if (!isProjectManager)
                throw new UnauthorizedAccessException("Only the worklog owner, the project manager, or Admin can edit this worklog.");
        }

        if (request.OverrideUserId.HasValue && request.OverrideUserId.Value != worklog.UserId)
        {
            if (!isAdmin)
                throw new UnauthorizedAccessException("Only Admin can re-assign a worklog to a different user.");

            var exists = await dbContext.Users.AnyAsync(u => u.Id == request.OverrideUserId.Value, cancellationToken);
            if (!exists)
                throw new NotFoundException(nameof(User), request.OverrideUserId.Value);

            worklog.UserId = request.OverrideUserId.Value;
        }

        if (request.TicketId != worklog.TicketId)
        {
            // Re-anchoring to a different ticket is only allowed within the
            // same project — a cross-project move would need authorization on
            // the destination project as well, which we don't grant here.
            await dbContext.GetTicketForProjectAsync(request.ProjectId, request.TicketId, cancellationToken);
            worklog.TicketId = request.TicketId;
        }

        worklog.Date = request.Date;
        worklog.Hours = request.Hours;
        worklog.Description = request.Description;
        worklog.IsBillable = request.IsBillable;
        worklog.Invoiced = request.Invoiced;
        worklog.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
