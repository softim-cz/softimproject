using MediatR;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Worklogs.DeleteWorklog;

public sealed record DeleteWorklogCommand(
    Guid ProjectId,
    Guid WorklogId) : IRequest, IRequireProjectAccess;

public sealed class DeleteWorklogCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService) : IRequestHandler<DeleteWorklogCommand>
{
    public async Task Handle(DeleteWorklogCommand request, CancellationToken cancellationToken)
    {
        var worklog = await dbContext.GetWorklogForProjectAsync(request.ProjectId, request.WorklogId, cancellationToken);

        var userId = currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Current user is not initialized.");

        if (worklog.UserId != userId
            && !currentUserService.IsInRole("Admin")
            && !currentUserService.IsInRole("Manager"))
        {
            throw new UnauthorizedAccessException("Only the worklog owner, Admin or Manager can delete this worklog.");
        }

        dbContext.Worklogs.Remove(worklog);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
