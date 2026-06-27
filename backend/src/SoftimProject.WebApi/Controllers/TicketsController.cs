using Microsoft.AspNetCore.Mvc;
using SoftimProject.Application.Features.Tickets.CreateTicket;
using SoftimProject.Application.Features.Tickets.DeleteTicket;
using SoftimProject.Application.Features.Tickets.GetTicketById;
using SoftimProject.Application.Features.Tickets.GetTicketByNumber;
using SoftimProject.Application.Common;
using SoftimProject.Application.Features.Tickets.GetTickets;
using SoftimProject.Application.Features.Tickets.MoveTicket;
using SoftimProject.Application.Features.Tickets.SetWatch;
using SoftimProject.Application.Features.Tickets.UpdateTicket;
using SoftimProject.Application.Features.Tickets;
using SoftimProject.Application.Features.Projects.GitHub;
using SoftimProject.Application.Features.Tickets.AiHistory;

namespace SoftimProject.WebApi.Controllers;

[Route("api/v{version:apiVersion}/projects/{projectId:guid}/tickets")]
public class TicketsController : ApiControllerBase
{
    /// <summary>Lists tickets in a project (paged) with optional filters (state, priority, assignee, type, due date, text search).</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<TicketListItemDto>>> GetAll(
        Guid projectId,
        [FromQuery] Guid? taskStateId = null,
        [FromQuery] Guid? ticketPriorityId = null,
        [FromQuery] Guid? assigneeId = null,
        [FromQuery] string? search = null,
        [FromQuery] Guid? taskTypeId = null,
        [FromQuery] string? taskStateName = null,
        [FromQuery] string? ticketPriorityName = null,
        [FromQuery] string? assignee = null,
        [FromQuery] string? taskTypeName = null,
        [FromQuery] DateOnly? dueDate = null,
        [FromQuery] string? sortField = null,
        [FromQuery] string? sortDirection = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        return Ok(await Mediator.Send(new GetTicketsQuery(
            projectId,
            taskStateId,
            ticketPriorityId,
            assigneeId,
            search,
            taskTypeId,
            taskStateName,
            ticketPriorityName,
            assignee,
            taskTypeName,
            dueDate,
            sortField,
            sortDirection,
            page,
            pageSize)));
    }

    /// <summary>Gets a single ticket by its id, including full detail (comments, checklist, sub-tickets, …).</summary>
    [HttpGet("{ticketId:guid}")]
    public async Task<ActionResult<TicketDetailDto>> GetById(Guid projectId, Guid ticketId)
    {
        return Ok(await Mediator.Send(new GetTicketByIdQuery(projectId, ticketId)));
    }

    /// <summary>Gets a ticket by its per-project number (the number in the ticket key, e.g. PRJ-42).</summary>
    [HttpGet("by-number/{number:int}")]
    public async Task<ActionResult<TicketDetailDto>> GetByNumber(Guid projectId, int number)
    {
        return Ok(await Mediator.Send(new GetTicketByNumberQuery(projectId, number)));
    }

    /// <summary>Creates a ticket in the project. Returns the new ticket id.</summary>
    [HttpPost]
    public async Task<ActionResult<Guid>> Create(Guid projectId, CreateTicketCommand command)
    {
        if (projectId != command.ProjectId) return BadRequest("Route projectId does not match command projectId.");
        var id = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { projectId, ticketId = id }, id);
    }

    /// <summary>Updates a ticket's fields (title, description, state, priority, assignee, dates, external attributes, …).</summary>
    [HttpPut("{ticketId:guid}")]
    public async Task<IActionResult> Update(Guid projectId, Guid ticketId, UpdateTicketCommand command)
    {
        if (projectId != command.ProjectId || ticketId != command.TicketId)
            return BadRequest("Route ids do not match command ids.");
        await Mediator.Send(command);
        return NoContent();
    }

    /// <summary>Moves a ticket to a kanban column/position (also updates its state per the column mapping).</summary>
    [HttpPut("{ticketId:guid}/move")]
    public async Task<IActionResult> Move(Guid projectId, Guid ticketId, MoveTicketCommand command)
    {
        if (projectId != command.ProjectId || ticketId != command.TicketId)
            return BadRequest("Route ids do not match command ids.");
        await Mediator.Send(command);
        return NoContent();
    }

    /// <summary>Deletes a ticket.</summary>
    [HttpDelete("{ticketId:guid}")]
    public async Task<IActionResult> Delete(Guid projectId, Guid ticketId)
    {
        await Mediator.Send(new DeleteTicketCommand(projectId, ticketId));
        return NoContent();
    }

    /// <summary>Sets whether the current user watches (follows) the ticket. Default is not watching.</summary>
    [HttpPut("{ticketId:guid}/watch")]
    public async Task<IActionResult> SetWatch(Guid projectId, Guid ticketId, [FromBody] SetWatchBody body)
    {
        await Mediator.Send(new SetTicketWatchCommand(projectId, ticketId, body.Watching));
        return NoContent();
    }

    public sealed record SetWatchBody(bool Watching);

    // --- GitHub integration per ticket ---

    [HttpGet("{ticketId:guid}/github/pull-requests")]
    public async Task<ActionResult<List<LinkedPullRequestDto>>> GetLinkedPullRequests(Guid projectId, Guid ticketId)
        => Ok(await Mediator.Send(new GetLinkedPullRequestsQuery(projectId, ticketId)));

    /// <summary>Lists commits linked to a ticket (discovered from commit messages referencing the ticket key).</summary>
    [HttpGet("{ticketId:guid}/github/commits")]
    public async Task<ActionResult<List<LinkedCommitDto>>> GetLinkedCommits(Guid projectId, Guid ticketId)
        => Ok(await Mediator.Send(new GetLinkedCommitsQuery(projectId, ticketId)));

    [HttpPost("{ticketId:guid}/github/create-branch")]
    public async Task<ActionResult<CreateTicketBranchResult>> CreateBranch(Guid projectId, Guid ticketId)
        => Ok(await Mediator.Send(new CreateTicketBranchCommand(projectId, ticketId)));

    // --- AI audit + manual re-run per ticket ---

    [HttpGet("{ticketId:guid}/ai/invocations")]
    public async Task<ActionResult<List<AiInvocationDto>>> GetAiHistory(Guid projectId, Guid ticketId)
        => Ok(await Mediator.Send(new GetTicketAiHistoryQuery(projectId, ticketId)));

    [HttpPost("{ticketId:guid}/ai/resummarize")]
    public async Task<ActionResult<object>> Resummarize(Guid projectId, Guid ticketId, [FromBody] ResummarizeTicketBody body)
    {
        var id = await Mediator.Send(new ResummarizeTicketCommand(projectId, ticketId, body.Reason));
        return Ok(new { invocationId = id });
    }

    public sealed record ResummarizeTicketBody(string Reason);
}


