using Microsoft.AspNetCore.Mvc;
using SoftimProject.Application.Features.Comments.GetComments;
using SoftimProject.Application.Features.Comments.ProjectComments;

namespace SoftimProject.WebApi.Controllers;

[Route("api/v{version:apiVersion}/projects/{projectId:guid}/comments")]
public class ProjectCommentsController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CommentDto>>> GetAll(Guid projectId)
    {
        return Ok(await Mediator.Send(new GetProjectCommentsQuery(projectId)));
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(Guid projectId, CreateProjectCommentCommand command)
    {
        if (projectId != command.ProjectId)
            return BadRequest("Route projectId does not match command projectId.");
        var id = await Mediator.Send(command);
        return Ok(id);
    }
}
