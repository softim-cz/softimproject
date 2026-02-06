using Microsoft.AspNetCore.Mvc;
using SoftimProject.Application.Features.Tickets.CreateTicket;
using SoftimProject.Application.Features.Tickets.DeleteTicket;
using SoftimProject.Application.Features.Tickets.GetTicketById;
using SoftimProject.Application.Features.Tickets.GetTickets;
using SoftimProject.Application.Features.Tickets.MoveTicket;
using SoftimProject.Application.Features.Tickets.UpdateTicket;
using SoftimProject.Domain.Enums;

namespace SoftimProject.WebApi.Controllers;

[Route("api/v{version:apiVersion}/projects/{projectId:guid}/tickets")]
public class TicketsController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<TicketListItemDto>>> GetAll(
        Guid projectId,
        [FromQuery] TicketStatus? status = null,
        [FromQuery] TicketPriority? priority = null,
        [FromQuery] Guid? assigneeId = null,
        [FromQuery] string? search = null)
    {
        return Ok(await Mediator.Send(new GetTicketsQuery(projectId, status, priority, assigneeId, search)));
    }

    [HttpGet("{ticketId:guid}")]
    public async Task<ActionResult<TicketDetailDto>> GetById(Guid projectId, Guid ticketId)
    {
        return Ok(await Mediator.Send(new GetTicketByIdQuery(projectId, ticketId)));
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
}
