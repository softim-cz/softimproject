using Microsoft.AspNetCore.Mvc;
using SoftimProject.Application.Features.Comments.CreateComment;
using SoftimProject.Application.Features.Comments.DeleteComment;
using SoftimProject.Application.Features.Comments.GetComments;
using SoftimProject.Application.Features.Comments.UpdateComment;

namespace SoftimProject.WebApi.Controllers;

[Route("api/v{version:apiVersion}/projects/{projectId:guid}/tickets/{ticketId:guid}/comments")]
public class CommentsController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        Guid projectId,
        Guid ticketId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100)
    {
        return Ok(await Mediator.Send(new GetCommentsQuery(projectId, ticketId, page, pageSize)));
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(Guid projectId, Guid ticketId, CreateCommentCommand command)
    {
        if (projectId != command.ProjectId || ticketId != command.TicketId)
            return BadRequest("Route ids do not match command ids.");
        var id = await Mediator.Send(command);
        return Ok(id);
    }

    [HttpPut("{commentId:guid}")]
    public async Task<IActionResult> Update(Guid projectId, Guid ticketId, Guid commentId, UpdateCommentCommand command)
    {
        if (projectId != command.ProjectId || ticketId != command.TicketId || commentId != command.CommentId)
            return BadRequest("Route ids do not match command ids.");
        await Mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{commentId:guid}")]
    public async Task<IActionResult> Delete(Guid projectId, Guid ticketId, Guid commentId)
    {
        await Mediator.Send(new DeleteCommentCommand(projectId, ticketId, commentId));
        return NoContent();
    }
}
