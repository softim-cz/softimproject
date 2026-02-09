using Microsoft.AspNetCore.Mvc;
using SoftimProject.Application.Features.Kanban.CreateBoard;
using SoftimProject.Application.Features.Kanban.CreateColumn;
using SoftimProject.Application.Features.Kanban.DeleteColumn;
using SoftimProject.Application.Features.Kanban.GetBoard;
using SoftimProject.Application.Features.Kanban.ReorderColumns;
using SoftimProject.Application.Features.Kanban.UpdateBoard;
using SoftimProject.Application.Features.Kanban.UpdateColumn;

namespace SoftimProject.WebApi.Controllers;

[Route("api/v{version:apiVersion}/projects/{projectId:guid}/boards")]
public class KanbanController : ApiControllerBase
{
    [HttpGet("~/api/v{version:apiVersion}/projects/{projectId:guid}/board")]
    public async Task<ActionResult<BoardDto>> GetDefaultBoard(Guid projectId)
    {
        return Ok(await Mediator.Send(new GetDefaultBoardQuery(projectId)));
    }

    [HttpGet("{boardId:guid}")]
    public async Task<ActionResult<BoardDto>> GetBoard(Guid projectId, Guid boardId)
    {
        return Ok(await Mediator.Send(new GetBoardQuery(boardId, projectId)));
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateBoard(Guid projectId, CreateBoardCommand command)
    {
        if (projectId != command.ProjectId) return BadRequest("Route projectId does not match command projectId.");
        var id = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetBoard), new { projectId, boardId = id }, id);
    }

    [HttpPut("{boardId:guid}")]
    public async Task<IActionResult> UpdateBoard(Guid projectId, Guid boardId, UpdateBoardCommand command)
    {
        if (projectId != command.ProjectId || boardId != command.BoardId)
            return BadRequest("Route ids do not match command ids.");
        await Mediator.Send(command);
        return NoContent();
    }

    // --- Columns ---

    [HttpPost("{boardId:guid}/columns")]
    public async Task<ActionResult<Guid>> CreateColumn(Guid projectId, Guid boardId, CreateColumnCommand command)
    {
        if (projectId != command.ProjectId || boardId != command.BoardId)
            return BadRequest("Route ids do not match command ids.");
        var id = await Mediator.Send(command);
        return Ok(id);
    }

    [HttpPut("{boardId:guid}/columns/{columnId:guid}")]
    public async Task<IActionResult> UpdateColumn(Guid projectId, Guid boardId, Guid columnId, UpdateColumnCommand command)
    {
        if (projectId != command.ProjectId || boardId != command.BoardId || columnId != command.ColumnId)
            return BadRequest("Route ids do not match command ids.");
        await Mediator.Send(command);
        return NoContent();
    }

    [HttpPut("{boardId:guid}/columns/reorder")]
    public async Task<IActionResult> ReorderColumns(Guid projectId, Guid boardId, ReorderColumnsCommand command)
    {
        if (projectId != command.ProjectId || boardId != command.BoardId)
            return BadRequest("Route ids do not match command ids.");
        await Mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{boardId:guid}/columns/{columnId:guid}")]
    public async Task<IActionResult> DeleteColumn(Guid projectId, Guid boardId, Guid columnId)
    {
        await Mediator.Send(new DeleteColumnCommand(projectId, boardId, columnId));
        return NoContent();
    }
}
