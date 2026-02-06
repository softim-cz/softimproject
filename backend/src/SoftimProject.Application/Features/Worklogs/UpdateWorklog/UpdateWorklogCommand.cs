using MediatR;
using Microsoft.EntityFrameworkCore;
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
        var worklog = await dbContext.Worklogs
            .FirstOrDefaultAsync(w => w.Id == request.WorklogId && w.ProjectId == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Worklog), request.WorklogId);

        worklog.Date = request.Date;
        worklog.Hours = request.Hours;
        worklog.Description = request.Description;
        worklog.IsBillable = request.IsBillable;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
