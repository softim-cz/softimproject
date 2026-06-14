using Microsoft.AspNetCore.Mvc;
using SoftimProject.Application.Features.Checklists.CreateChecklistItem;
using SoftimProject.Application.Features.Checklists.DeleteChecklistItem;
using SoftimProject.Application.Features.Checklists.UpdateChecklistItem;

namespace SoftimProject.WebApi.Controllers;

[Route("api/v{version:apiVersion}/projects/{projectId:guid}/tickets/{ticketId:guid}/checklist-items")]
public class ChecklistItemsController : ApiControllerBase
{
    /// <summary>Adds a checklist item to a ticket.</summary>
    [HttpPost]
    public async Task<ActionResult<Guid>> Create(Guid projectId, Guid ticketId, CreateChecklistItemCommand command)
    {
        if (projectId != command.ProjectId || ticketId != command.TicketId)
            return BadRequest("Route ids do not match command ids.");
        var id = await Mediator.Send(command);
        return Ok(id);
    }

    /// <summary>Updates a checklist item (text, completion, position).</summary>
    [HttpPut("{itemId:guid}")]
    public async Task<IActionResult> Update(Guid projectId, Guid ticketId, Guid itemId, UpdateChecklistItemCommand command)
    {
        if (projectId != command.ProjectId || ticketId != command.TicketId || itemId != command.ItemId)
            return BadRequest("Route ids do not match command ids.");
        await Mediator.Send(command);
        return NoContent();
    }

    /// <summary>Deletes a checklist item.</summary>
    [HttpDelete("{itemId:guid}")]
    public async Task<IActionResult> Delete(Guid projectId, Guid ticketId, Guid itemId)
    {
        await Mediator.Send(new DeleteChecklistItemCommand(projectId, ticketId, itemId));
        return NoContent();
    }
}
