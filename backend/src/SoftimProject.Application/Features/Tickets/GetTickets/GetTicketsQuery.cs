using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Tickets.GetTickets;

public sealed record TicketUserDto(
    Guid Id,
    string DisplayName);

public sealed record TicketListItemDto(
    Guid Id,
    int Number,
    string Key,
    string Title,
    Guid TicketPriorityId,
    string TicketPriorityName,
    string TicketPriorityColor,
    Guid TaskStateId,
    string TaskStateName,
    string TaskStateColor,
    Guid? AssigneeId,
    TicketUserDto? Assignee,
    Guid? ColumnId,
    double Position,
    DateOnly? DueDate,
    decimal? EstimatedHours,
    Guid? TaskTypeId,
    string? TaskTypeName,
    string? TaskTypeIcon,
    Guid? ParentTicketId,
    decimal CumulativeWorkedHours,
    int CommentsCount,
    int AttachmentsCount,
    DateTime CreatedAt);

public sealed record GetTicketsQuery(
    Guid ProjectId,
    Guid? TaskStateId = null,
    Guid? TicketPriorityId = null,
    Guid? AssigneeId = null,
    string? SearchTerm = null,
    Guid? TaskTypeId = null,
    string? TaskStateName = null,
    string? TicketPriorityName = null,
    string? AssigneeName = null,
    string? TaskTypeName = null,
    DateOnly? DueDate = null) : IRequest<List<TicketListItemDto>>, IRequireProjectAccess;

public sealed class GetTicketsQueryHandler(
    IApplicationDbContext dbContext) : IRequestHandler<GetTicketsQuery, List<TicketListItemDto>>
{
    public async Task<List<TicketListItemDto>> Handle(GetTicketsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Tickets
            .AsNoTracking()
            .Where(t => t.ProjectId == request.ProjectId)
            .AsQueryable();

        if (request.TaskStateId.HasValue)
            query = query.Where(t => t.TaskStateId == request.TaskStateId.Value);

        if (request.TicketPriorityId.HasValue)
            query = query.Where(t => t.TicketPriorityId == request.TicketPriorityId.Value);

        if (request.AssigneeId.HasValue)
            query = query.Where(t => t.AssigneeId == request.AssigneeId.Value);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            query = query.Where(t => t.Title.Contains(request.SearchTerm));

        if (request.TaskTypeId.HasValue)
            query = query.Where(t => t.TaskTypeId == request.TaskTypeId.Value);

        if (!string.IsNullOrWhiteSpace(request.TaskStateName))
            query = query.Where(t => t.TaskState.Name == request.TaskStateName);

        if (!string.IsNullOrWhiteSpace(request.TicketPriorityName))
            query = query.Where(t => t.TicketPriority.Name == request.TicketPriorityName);

        if (!string.IsNullOrWhiteSpace(request.AssigneeName))
            query = query.Where(t => t.Assignee != null && t.Assignee.DisplayName == request.AssigneeName);

        if (!string.IsNullOrWhiteSpace(request.TaskTypeName))
            query = query.Where(t => t.TaskType != null && t.TaskType.Name == request.TaskTypeName);

        if (request.DueDate.HasValue)
            query = query.Where(t => t.DueDate == request.DueDate.Value);

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TicketListItemDto(
                t.Id,
                t.Number,
                t.Project.Code + "-" + t.Number,
                t.Title,
                t.TicketPriorityId,
                t.TicketPriority.Name,
                t.TicketPriority.Color,
                t.TaskStateId,
                t.TaskState.Name,
                t.TaskState.Color,
                t.AssigneeId,
                t.Assignee != null ? new TicketUserDto(t.Assignee.Id, t.Assignee.DisplayName) : null,
                t.ColumnId,
                t.Position,
                t.DueDate,
                t.EstimatedHours,
                t.TaskTypeId,
                t.TaskType != null ? t.TaskType.Name : null,
                t.TaskType != null ? t.TaskType.Icon : null,
                t.ParentTicketId,
                t.CumulativeWorkedHours,
                t.Comments.Count,
                t.Attachments.Count,
                t.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
