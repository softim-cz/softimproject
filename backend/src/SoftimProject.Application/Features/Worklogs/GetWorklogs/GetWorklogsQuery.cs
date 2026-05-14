using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Worklogs.GetWorklogs;

public sealed record WorklogUserDto(
    Guid Id,
    string DisplayName);

public sealed record WorklogDto(
    Guid Id,
    Guid ProjectId,
    string ProjectName,
    Guid TicketId,
    string TicketTitle,
    Guid UserId,
    WorklogUserDto User,
    DateOnly Date,
    decimal Hours,
    string Description,
    WorklogSource Source,
    bool IsBillable,
    decimal? HourlyRateSnapshot,
    string? AiSummary,
    string? Invoiced,
    DateTime CreatedAt);

public sealed record GetWorklogsQuery(
    Guid? ProjectId = null,
    Guid? TicketId = null,
    DateOnly? From = null,
    DateOnly? To = null,
    Guid? UserId = null,
    int Page = 1,
    int PageSize = 50) : IRequest<PagedResult<WorklogDto>>;

public sealed class GetWorklogsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService) : IRequestHandler<GetWorklogsQuery, PagedResult<WorklogDto>>
{
    public async Task<PagedResult<WorklogDto>> Handle(GetWorklogsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Worklogs.AsNoTracking().AsQueryable();

        if (request.ProjectId.HasValue)
            query = query.Where(w => w.Ticket.ProjectId == request.ProjectId.Value);

        if (request.TicketId.HasValue)
            query = query.Where(w => w.TicketId == request.TicketId.Value);

        if (request.From.HasValue)
            query = query.Where(w => w.Date >= request.From.Value);

        if (request.To.HasValue)
            query = query.Where(w => w.Date <= request.To.Value);

        if (request.UserId.HasValue)
            query = query.Where(w => w.UserId == request.UserId.Value);

        // Without a ProjectId filter, non-admin callers only ever see their own worklogs.
        // When a ProjectId is provided, row-level visibility is enforced by project membership
        // (the controller-level IRequireProjectAccess guard, applied per request).
        if (!currentUserService.IsInRole("Admin")
            && !request.ProjectId.HasValue
            && currentUserService.UserId.HasValue)
        {
            query = query.Where(w => w.UserId == currentUserService.UserId.Value);
        }

        var ordered = query
            .OrderByDescending(w => w.Date)
            .ThenByDescending(w => w.CreatedAt);

        var totalCount = await ordered.CountAsync(cancellationToken);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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
            .ToListAsync(cancellationToken);

        return new PagedResult<WorklogDto>(items, totalCount, page, pageSize);
    }
}
