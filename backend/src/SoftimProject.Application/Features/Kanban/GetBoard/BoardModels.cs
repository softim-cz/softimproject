using System.Linq.Expressions;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Application.Features.Kanban.GetBoard;

public sealed record BoardTicketDto(
    Guid Id,
    int Number,
    string Key,
    string Title,
    Guid TicketPriorityId,
    string TicketPriorityName,
    string TicketPriorityColor,
    double Position,
    Guid? AssigneeId,
    string? AssigneeDisplayName,
    DateOnly? DueDate,
    decimal? EstimatedHours,
    Guid? TaskTypeId,
    string? TaskTypeName,
    string? TaskTypeIcon,
    Guid TaskStateId,
    string TaskStateName,
    string TaskStateColor);

public sealed record BoardColumnTaskStateDto(
    Guid Id,
    string Name,
    string Color);

public sealed record BoardColumnDto(
    Guid Id,
    string Name,
    int Position,
    int? WipLimit,
    string? Color,
    bool IsVisible,
    List<BoardColumnTaskStateDto> TaskStates,
    List<BoardTicketDto> Tickets);

public sealed record BoardDto(
    Guid Id,
    string Name,
    bool IsDefault,
    Guid ProjectId,
    List<BoardColumnDto> Columns);

internal static class BoardProjections
{
    // Sdílené promítání nástěnky do DTO. Řazení (sloupce/stavy/tikety) je součástí
    // výrazu, aby se promítlo přímo v databázi a výsledek byl konzistentní napříč
    // dotazy (konkrétní nástěnka i výchozí nástěnka projektu).
    public static readonly Expression<Func<KanbanBoard, BoardDto>> Detail = b => new BoardDto(
        b.Id,
        b.Name,
        b.IsDefault,
        b.ProjectId,
        b.Columns.OrderBy(c => c.Position).Select(c => new BoardColumnDto(
            c.Id,
            c.Name,
            c.Position,
            c.WipLimit,
            c.Color,
            c.IsVisible,
            c.MapsToTaskStates.OrderBy(ts => ts.SortOrder).Select(ts => new BoardColumnTaskStateDto(
                ts.Id,
                ts.Name,
                ts.Color)).ToList(),
            c.Tickets.OrderBy(t => t.Position).Select(t => new BoardTicketDto(
                t.Id,
                t.Number,
                t.Project.Code + "-" + t.Number,
                t.Title,
                t.TicketPriorityId,
                t.TicketPriority.Name,
                t.TicketPriority.Color,
                t.Position,
                t.AssigneeId,
                t.Assignee != null ? t.Assignee.DisplayName : null,
                t.DueDate,
                t.EstimatedHours,
                t.TaskTypeId,
                t.TaskType != null ? t.TaskType.Name : null,
                t.TaskType != null ? t.TaskType.Icon : null,
                t.TaskStateId,
                t.TaskState.Name,
                t.TaskState.Color)).ToList()
        )).ToList());
}
