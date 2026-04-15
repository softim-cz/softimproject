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
    bool IsBillable) : IRequest, IRequireProjectAccess;

public sealed class UpdateWorklogCommandHandler(
    IApplicationDbContext dbContext) : IRequestHandler<UpdateWorklogCommand>
{
    public async Task Handle(UpdateWorklogCommand request, CancellationToken cancellationToken)
    {
        var worklog = await dbContext.GetWorklogForProjectAsync(request.ProjectId, request.WorklogId, cancellationToken);

        worklog.Date = request.Date;
        worklog.Hours = request.Hours;
        worklog.Description = request.Description;
        worklog.IsBillable = request.IsBillable;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
