using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Dashboard;

public sealed record DashboardStatsDto(
    List<TicketsByStateDto> TicketsByState,
    List<MyOpenTicketDto> MyOpenTickets);

public sealed record TicketsByStateDto(
    Guid StateId,
    string StateName,
    string StateColor,
    int SortOrder,
    int Count);

public sealed record MyOpenTicketDto(
    Guid Id,
    int Number,
    string Key,
    string Title,
    Guid ProjectId,
    string ProjectName,
    string ProjectCode,
    string TaskStateName,
    string TaskStateColor,
    string TicketPriorityName,
    string TicketPriorityColor,
    DateOnly? DueDate);

public sealed record GetDashboardStatsQuery : IRequest<DashboardStatsDto>;

public sealed class GetDashboardStatsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService) : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
{
    public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Current user is not initialized.");

        var accessibleProjectIds = await dbContext.ProjectMembers
            .Where(pm => pm.UserId == userId)
            .Select(pm => pm.ProjectId)
            .ToListAsync(cancellationToken);

        var countsByState = await dbContext.Tickets
            .Where(t => accessibleProjectIds.Contains(t.ProjectId))
            .GroupBy(t => t.TaskStateId)
            .Select(g => new { StateId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var stateIds = countsByState.Select(c => c.StateId).ToList();

        var states = await dbContext.TaskStates
            .Where(ts => stateIds.Contains(ts.Id))
            .OrderBy(ts => ts.SortOrder)
            .ToListAsync(cancellationToken);

        var ticketsByState = states
            .Select(ts => new TicketsByStateDto(
                ts.Id,
                ts.Name,
                ts.Color,
                ts.SortOrder,
                countsByState.First(c => c.StateId == ts.Id).Count))
            .ToList();

        var myOpenTickets = await dbContext.Tickets
            .Where(t => t.AssigneeId == userId && !t.TaskState.IsClosedState)
            .OrderBy(t => t.DueDate == null ? 1 : 0)
            .ThenBy(t => t.DueDate)
            .ThenByDescending(t => t.TicketPriority.SortOrder)
            .Take(10)
            .Select(t => new MyOpenTicketDto(
                t.Id,
                t.Number,
                t.Project.Code + "-" + t.Number,
                t.Title,
                t.ProjectId,
                t.Project.Name,
                t.Project.Code,
                t.TaskState.Name,
                t.TaskState.Color,
                t.TicketPriority.Name,
                t.TicketPriority.Color,
                t.DueDate))
            .ToListAsync(cancellationToken);

        return new DashboardStatsDto(ticketsByState, myOpenTickets);
    }
}
