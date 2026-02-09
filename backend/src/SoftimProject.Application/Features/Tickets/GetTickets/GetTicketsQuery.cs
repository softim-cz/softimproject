using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Tickets.GetTickets;

public sealed record TicketListItemDto(
    Guid Id,
    string Title,
    TicketPriority Priority,
    TicketStatus Status,
    Guid? AssigneeId,
    string? AssigneeDisplayName,
    Guid? ColumnId,
    double Position,
    DateOnly? DueDate,
    decimal? EstimatedHours,
    Guid? TaskTypeId,
    string? TaskTypeName,
    string? TaskTypeIcon,
    Guid? TaskStateId,
    string? TaskStateName,
    string? TaskStateColor,
    Guid? ParentTicketId,
    DateTime CreatedAt);

public sealed record GetTicketsQuery(
    Guid ProjectId,
    TicketStatus? Status = null,
    TicketPriority? Priority = null,
    Guid? AssigneeId = null,
    string? SearchTerm = null,
    Guid? TaskTypeId = null,
    Guid? TaskStateId = null) : IRequest<List<TicketListItemDto>>, IRequireProjectAccess;

public sealed class GetTicketsQueryHandler(
    IApplicationDbContext dbContext) : IRequestHandler<GetTicketsQuery, List<TicketListItemDto>>
{
    public async Task<List<TicketListItemDto>> Handle(GetTicketsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Tickets
            .Where(t => t.ProjectId == request.ProjectId)
            .AsQueryable();

        if (request.Status.HasValue)
            query = query.Where(t => t.Status == request.Status.Value);

        if (request.Priority.HasValue)
            query = query.Where(t => t.Priority == request.Priority.Value);

        if (request.AssigneeId.HasValue)
            query = query.Where(t => t.AssigneeId == request.AssigneeId.Value);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            query = query.Where(t => t.Title.Contains(request.SearchTerm));

        if (request.TaskTypeId.HasValue)
            query = query.Where(t => t.TaskTypeId == request.TaskTypeId.Value);

        if (request.TaskStateId.HasValue)
            query = query.Where(t => t.TaskStateId == request.TaskStateId.Value);

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TicketListItemDto(
                t.Id,
                t.Title,
                t.Priority,
                t.Status,
                t.AssigneeId,
                t.Assignee != null ? t.Assignee.DisplayName : null,
                t.ColumnId,
                t.Position,
                t.DueDate,
                t.EstimatedHours,
                t.TaskTypeId,
                t.TaskType != null ? t.TaskType.Name : null,
                t.TaskType != null ? t.TaskType.Icon : null,
                t.TaskStateId,
                t.TaskState != null ? t.TaskState.Name : null,
                t.TaskState != null ? t.TaskState.Color : null,
                t.ParentTicketId,
                t.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
