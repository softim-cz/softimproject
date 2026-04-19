using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.WebApi.Controllers;

[ApiController]
[ApiVersion("1.0")]
[AllowAnonymous]
[Route("api/v{version:apiVersion}/portal/{token}")]
public class PortalController(IApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(string token, CancellationToken ct)
    {
        var project = await dbContext.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ClientAccessToken == token && p.ClientAccessEnabled, ct)
            ?? throw new NotFoundException("Portal", token);

        var board = await dbContext.KanbanBoards
            .AsNoTracking()
            .Where(b => b.ProjectId == project.Id && b.IsDefault)
            .Select(b => new PortalBoardDto(
                b.Id,
                b.ProjectId,
                b.Name,
                b.IsDefault,
                b.Columns.OrderBy(c => c.Position).Select(c => new PortalColumnDto(
                    c.Id,
                    c.BoardId,
                    c.Name,
                    c.Position,
                    c.WipLimit,
                    c.MapsToTaskStates.Select(ts => new PortalTaskStateDto(ts.Id, ts.Name, ts.Color)).ToList(),
                    c.Tickets
                        .OrderBy(t => t.Position)
                        .Select(t => new PortalTicketDto(
                            t.Id,
                            t.Number,
                            project.Code + "-" + t.Number,
                            t.ProjectId,
                            c.Id,
                            t.Title,
                            t.TicketPriorityId,
                            t.TicketPriority.Name,
                            t.TicketPriority.Color,
                            t.TaskStateId,
                            t.TaskState.Name,
                            t.TaskState.Color,
                            t.Position,
                            t.AssigneeId,
                            t.Assignee != null ? new PortalUserDto(t.Assignee.Id, t.Assignee.DisplayName) : null,
                            t.DueDate))
                        .ToList()))
                    .ToList()))
            .FirstOrDefaultAsync(ct);

        var totalHours = await dbContext.Worklogs
            .Where(w => w.ProjectId == project.Id && w.IsBillable)
            .SumAsync(w => (decimal?)w.Hours, ct) ?? 0m;

        return Ok(new PortalResponseDto(
            new PortalProjectDto(
                project.Id,
                project.Name,
                project.Code,
                project.Description,
                project.Status.ToString(),
                project.BudgetHours,
                project.SpentHours,
                project.HealthScore,
                project.IsOverBudget,
                project.IsOverDeadline),
            board,
            totalHours,
            Array.Empty<object>()));
    }
}

public sealed record PortalResponseDto(
    PortalProjectDto Project,
    PortalBoardDto? Board,
    decimal TotalHours,
    IReadOnlyList<object> Comments);

public sealed record PortalProjectDto(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    string Status,
    decimal? BudgetHours,
    decimal SpentHours,
    int HealthScore,
    bool IsOverBudget,
    bool IsOverDeadline);

public sealed record PortalBoardDto(
    Guid Id,
    Guid ProjectId,
    string Name,
    bool IsDefault,
    List<PortalColumnDto> Columns);

public sealed record PortalColumnDto(
    Guid Id,
    Guid BoardId,
    string Name,
    int Position,
    int? WipLimit,
    List<PortalTaskStateDto> TaskStates,
    List<PortalTicketDto> Tickets);

public sealed record PortalTaskStateDto(
    Guid Id,
    string Name,
    string Color);

public sealed record PortalTicketDto(
    Guid Id,
    int Number,
    string Key,
    Guid ProjectId,
    Guid? ColumnId,
    string Title,
    Guid TicketPriorityId,
    string TicketPriorityName,
    string TicketPriorityColor,
    Guid TaskStateId,
    string TaskStateName,
    string TaskStateColor,
    double Position,
    Guid? AssigneeId,
    PortalUserDto? Assignee,
    DateOnly? DueDate);

public sealed record PortalUserDto(
    Guid Id,
    string DisplayName);
