using MediatR;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Worklogs.UpdateWorklog;

public sealed record UpdateWorklogCommand(
    Guid ProjectId,
    Guid WorklogId,
    DateOnly Date,
    decimal Hours,
    string? Description,
    bool IsBillable,
    string? Invoiced) : IRequest, IRequireProjectAccess;

public sealed class UpdateWorklogCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService) : IRequestHandler<UpdateWorklogCommand>
{
    public async Task Handle(UpdateWorklogCommand request, CancellationToken cancellationToken)
    {
        var worklog = await dbContext.GetWorklogForProjectAsync(request.ProjectId, request.WorklogId, cancellationToken);

        var userId = currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Current user is not initialized.");

        if (worklog.UserId != userId
            && !currentUserService.IsInRole("Admin")
            && !currentUserService.IsInRole("Manager"))
        {
            throw new UnauthorizedAccessException("Only the worklog owner, Admin or Manager can edit this worklog.");
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
