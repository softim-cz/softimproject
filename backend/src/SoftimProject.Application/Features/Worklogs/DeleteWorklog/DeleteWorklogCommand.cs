using MediatR;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Worklogs.DeleteWorklog;

public sealed record DeleteWorklogCommand(
    Guid ProjectId,
    Guid WorklogId) : IRequest, IRequireProjectAccess;

public sealed class DeleteWorklogCommandHandler(
    IApplicationDbContext dbContext) : IRequestHandler<DeleteWorklogCommand>
{
    public async Task Handle(DeleteWorklogCommand request, CancellationToken cancellationToken)
    {
        var worklog = await dbContext.GetWorklogForProjectAsync(request.ProjectId, request.WorklogId, cancellationToken);

        dbContext.Worklogs.Remove(worklog);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
