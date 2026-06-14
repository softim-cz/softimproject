using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Features.Worklogs.GetWorklogs;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Worklogs.GetWorklogById;

public sealed record GetWorklogByIdQuery(Guid WorklogId) : IRequest<WorklogDto>;

public sealed class GetWorklogByIdQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService) : IRequestHandler<GetWorklogByIdQuery, WorklogDto>
{
    public async Task<WorklogDto> Handle(GetWorklogByIdQuery request, CancellationToken cancellationToken)
    {
        var dto = await dbContext.Worklogs
            .AsNoTracking()
            .Where(w => w.Id == request.WorklogId)
            .Select(w => new WorklogDto(
                w.Id,
                w.Ticket.ProjectId,
                w.Ticket.Project.Name,
                w.TicketId,
                w.Ticket.Title,
                w.UserId,
                new WorklogUserDto(w.UserId, w.User.DisplayName),
                w.Date,
                w.Hours,
                w.Description,
                w.Source,
                w.IsBillable,
                w.HourlyRateSnapshot,
                w.AiSummary,
                w.Invoiced,
                w.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Worklog), request.WorklogId);

        // Visible to the owner, Admins, or members of the worklog's project.
        // Treat "no access" as not-found so we don't leak existence.
        var allowed = dto.UserId == currentUserService.UserId
            || currentUserService.IsInRole("Admin")
            || await currentUserService.HasProjectAccessAsync(dto.ProjectId, cancellationToken);
        if (!allowed)
            throw new NotFoundException(nameof(Domain.Entities.Worklog), request.WorklogId);

        return dto;
    }
}
