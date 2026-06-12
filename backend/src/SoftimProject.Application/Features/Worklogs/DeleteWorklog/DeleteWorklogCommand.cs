using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Worklogs.DeleteWorklog;

public sealed record DeleteWorklogCommand(
    Guid ProjectId,
    Guid WorklogId) : IRequest, IRequireProjectRole
{
    public ProjectRole RequiredProjectRole => ProjectRole.Developer;
}

public sealed class DeleteWorklogCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService) : IRequestHandler<DeleteWorklogCommand>
{
    public async Task Handle(DeleteWorklogCommand request, CancellationToken cancellationToken)
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
                throw new UnauthorizedAccessException("Only the worklog owner, the project manager, or Admin can delete this worklog.");
        }

        var ticketId = worklog.TicketId;

        dbContext.Worklogs.Remove(worklog);
        await dbContext.SaveChangesAsync(cancellationToken);

        await CumulativeWorkedHoursCalculator.RecalculateUpwardAsync(dbContext, ticketId, cancellationToken);
    }
}
