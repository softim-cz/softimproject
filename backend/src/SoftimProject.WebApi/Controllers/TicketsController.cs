using Microsoft.AspNetCore.Mvc;
using SoftimProject.Application.Features.Tickets.CreateTicket;
using SoftimProject.Application.Features.Tickets.DeleteTicket;
using SoftimProject.Application.Features.Tickets.GetTicketById;
using SoftimProject.Application.Features.Tickets.GetTicketByNumber;
using SoftimProject.Application.Common;
using SoftimProject.Application.Features.Tickets.GetTickets;
using SoftimProject.Application.Features.Tickets.MoveTicket;
using SoftimProject.Application.Features.Tickets.UpdateTicket;
using SoftimProject.Application.Features.Tickets;
using SoftimProject.Application.Features.Projects.GitHub;
using SoftimProject.Application.Features.Tickets.AiHistory;

namespace SoftimProject.WebApi.Controllers;

[Route("api/v{version:apiVersion}/projects/{projectId:guid}/tickets")]
public class TicketsController : ApiControllerBase
{
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
            page,
            pageSize)));
    }

    [HttpGet("{ticketId:guid}")]
    public async Task<ActionResult<TicketDetailDto>> GetById(Guid projectId, Guid ticketId)
    {
        return Ok(await Mediator.Send(new GetTicketByIdQuery(projectId, ticketId)));
    }

    [HttpGet("by-number/{number:int}")]
    public async Task<ActionResult<TicketDetailDto>> GetByNumber(Guid projectId, int number)
    {
        return Ok(await Mediator.Send(new GetTicketByNumberQuery(projectId, number)));
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(Guid projectId, CreateTicketCommand command)
    {
        if (projectId != command.ProjectId) return BadRequest("Route projectId does not match command projectId.");
        var id = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { projectId, ticketId = id }, id);
    }

    [HttpPut("{ticketId:guid}")]
    public async Task<IActionResult> Update(Guid projectId, Guid ticketId, UpdateTicketCommand command)
    {
        if (projectId != command.ProjectId || ticketId != command.TicketId)
            return BadRequest("Route ids do not match command ids.");
        await Mediator.Send(command);
        return NoContent();
    }

    [HttpPut("{ticketId:guid}/move")]
    public async Task<IActionResult> Move(Guid projectId, Guid ticketId, MoveTicketCommand command)
    {
        if (projectId != command.ProjectId || ticketId != command.TicketId)
            return BadRequest("Route ids do not match command ids.");
        await Mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{ticketId:guid}")]
    public async Task<IActionResult> Delete(Guid projectId, Guid ticketId)
    {
        await Mediator.Send(new DeleteTicketCommand(projectId, ticketId));
        return NoContent();
    }

    // --- GitHub integration per ticket ---

    [HttpGet("{ticketId:guid}/github/pull-requests")]
    public async Task<ActionResult<List<LinkedPullRequestDto>>> GetLinkedPullRequests(Guid projectId, Guid ticketId)
        => Ok(await Mediator.Send(new GetLinkedPullRequestsQuery(projectId, ticketId)));

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


