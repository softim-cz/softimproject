using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Worklogs.UpdateWorklog;

public sealed record UpdateWorklogCommand(
    Guid ProjectId,
    Guid WorklogId,
    DateOnly Date,
    decimal Hours,
    string? Description,
    bool IsBillable,
    string? Invoiced) : IRequest, IRequireProjectRole
{
    public ProjectRole RequiredProjectRole => ProjectRole.Developer;
}

public sealed class UpdateWorklogCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService) : IRequestHandler<UpdateWorklogCommand>
{
    public async Task Handle(UpdateWorklogCommand request, CancellationToken cancellationToken)
    {
        var worklog = await dbContext.GetWorklogForProjectAsync(request.ProjectId, request.WorklogId, cancellationToken);

        var userId = currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Current user is not initialized.");

        if (worklog.UserId != userId && !currentUserService.IsInRole("Admin"))
        {
            var isProjectManager = await dbContext.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == request.ProjectId
                    && pm.UserId == userId
                    && pm.Role == ProjectRole.ProjectManager, cancellationToken);
            if (!isProjectManager)
                throw new UnauthorizedAccessException("Only the worklog owner, the project manager, or Admin can edit this worklog.");
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
