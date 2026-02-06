using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.WebApi.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/client/{token}")]
public class ClientPortalController(IApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet("project")]
    public async Task<IActionResult> GetProject(string token)
    {
        var project = await dbContext.Projects
            .FirstOrDefaultAsync(p => p.ClientAccessToken == token && p.ClientAccessEnabled)
            ?? throw new NotFoundException("Project", token);

        return Ok(new ClientProjectDto(
            project.Id,
            project.Name,
            project.Code,
            project.Description,
            project.Status,
            project.StartDate,
            project.EndDate,
            project.DeadlineDate,
            project.HealthScore,
            project.IsOverBudget,
            project.IsOverDeadline));
    }

    [HttpGet("board")]
    public async Task<IActionResult> GetBoard(string token)
    {
        var project = await dbContext.Projects
            .FirstOrDefaultAsync(p => p.ClientAccessToken == token && p.ClientAccessEnabled)
            ?? throw new NotFoundException("Project", token);

        var board = await dbContext.KanbanBoards
            .Include(b => b.Columns.OrderBy(c => c.Position))
                .ThenInclude(c => c.Tickets.OrderBy(t => t.Position))
                    .ThenInclude(t => t.Assignee)
            .FirstOrDefaultAsync(b => b.ProjectId == project.Id && b.IsDefault)
            ?? throw new NotFoundException("KanbanBoard", project.Id);

        var columns = board.Columns.Select(c => new ClientBoardColumnDto(
            c.Id,
            c.Name,
            c.Position,
            c.MapsToStatus,
            c.Tickets
                .Where(t => t.Comments.All(co => !co.IsInternal))
                .Select(t => new ClientBoardTicketDto(
                    t.Id,
                    t.Title,
                    t.Priority,
                    t.Status,
                    t.Assignee?.DisplayName,
                    t.DueDate))
                .ToList()
        )).ToList();

        return Ok(new ClientBoardDto(board.Id, board.Name, columns));
    }

    [HttpGet("hours")]
    public async Task<IActionResult> GetHours(string token)
    {
        var project = await dbContext.Projects
            .FirstOrDefaultAsync(p => p.ClientAccessToken == token && p.ClientAccessEnabled)
            ?? throw new NotFoundException("Project", token);

        var worklogs = await dbContext.Worklogs
            .Where(w => w.ProjectId == project.Id && w.IsBillable)
            .GroupBy(w => w.UserId)
            .Select(g => new ClientWorklogSummaryDto(
                g.First().User.DisplayName,
                g.Sum(w => w.Hours)))
            .ToListAsync();

        var totalHours = worklogs.Sum(w => w.Hours);

        return Ok(new ClientHoursSummaryDto(
            project.BudgetHours,
            project.SpentHours,
            totalHours,
            worklogs));
    }
}

public sealed record ClientProjectDto(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    ProjectStatus Status,
    DateOnly? StartDate,
    DateOnly? EndDate,
    DateOnly? DeadlineDate,
    int HealthScore,
    bool IsOverBudget,
    bool IsOverDeadline);

public sealed record ClientBoardDto(
    Guid Id,
    string Name,
    List<ClientBoardColumnDto> Columns);

public sealed record ClientBoardColumnDto(
    Guid Id,
    string Name,
    int Position,
    TicketStatus MapsToStatus,
    List<ClientBoardTicketDto> Tickets);

public sealed record ClientBoardTicketDto(
    Guid Id,
    string Title,
    TicketPriority Priority,
    TicketStatus Status,
    string? AssigneeDisplayName,
    DateOnly? DueDate);

public sealed record ClientWorklogSummaryDto(
    string UserDisplayName,
    decimal Hours);

public sealed record ClientHoursSummaryDto(
    decimal? BudgetHours,
    decimal SpentHours,
    decimal BillableHours,
    List<ClientWorklogSummaryDto> ByUser);
