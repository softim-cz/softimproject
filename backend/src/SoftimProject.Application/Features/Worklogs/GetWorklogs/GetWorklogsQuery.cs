using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Worklogs.GetWorklogs;

public sealed record WorklogDto(
    Guid Id,
    Guid ProjectId,
    string ProjectName,
    Guid? TicketId,
    string? TicketTitle,
    Guid UserId,
    string UserDisplayName,
    DateOnly Date,
    decimal Hours,
    string? Description,
    WorklogSource Source,
    bool IsBillable,
    decimal? HourlyRateSnapshot,
    string? AiSummary,
    string? Invoiced,
    DateTime CreatedAt);

public sealed record GetWorklogsQuery(
    Guid? ProjectId = null,
    DateOnly? From = null,
    DateOnly? To = null,
    Guid? UserId = null) : IRequest<List<WorklogDto>>;

public sealed class GetWorklogsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService) : IRequestHandler<GetWorklogsQuery, List<WorklogDto>>
{
    public async Task<List<WorklogDto>> Handle(GetWorklogsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Worklogs.AsQueryable();

        if (request.ProjectId.HasValue)
            query = query.Where(w => w.ProjectId == request.ProjectId.Value);

        if (request.From.HasValue)
            query = query.Where(w => w.Date >= request.From.Value);

        if (request.To.HasValue)
            query = query.Where(w => w.Date <= request.To.Value);

        if (request.UserId.HasValue)
            query = query.Where(w => w.UserId == request.UserId.Value);

        // Non-admin, non-manager users can only see their own worklogs unless scoped to a project
        if (!currentUserService.IsInRole("Admin") && !request.ProjectId.HasValue && currentUserService.UserId.HasValue)
        {
            query = query.Where(w => w.UserId == currentUserService.UserId.Value);
        }

        return await query
            .OrderByDescending(w => w.Date)
            .ThenByDescending(w => w.CreatedAt)
            .Select(w => new WorklogDto(
                w.Id,
                w.ProjectId,
                w.Project.Name,
                w.TicketId,
                w.Ticket != null ? w.Ticket.Title : null,
                w.UserId,
                w.User.DisplayName,
                w.Date,
                w.Hours,
                w.Description,
                w.Source,
                w.IsBillable,
                w.HourlyRateSnapshot,
                w.AiSummary,
                w.Invoiced,
                w.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
