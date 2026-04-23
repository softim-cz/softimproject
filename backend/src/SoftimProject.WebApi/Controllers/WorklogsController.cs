using Microsoft.AspNetCore.Mvc;
using SoftimProject.Application.Features.Worklogs.CreateWorklog;
using SoftimProject.Application.Features.Worklogs.DeleteWorklog;
using SoftimProject.Application.Features.Worklogs.GetWorklogs;
using SoftimProject.Application.Features.Worklogs.UpdateWorklog;

namespace SoftimProject.WebApi.Controllers;

public class WorklogsController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? projectId = null,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        return Ok(await Mediator.Send(new GetWorklogsQuery(projectId, from, to, userId, page, pageSize)));
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateWorklogCommand command)
    {
        var id = await Mediator.Send(command);
        return Ok(id);
    }

    [HttpPut("{worklogId:guid}")]
    public async Task<IActionResult> Update(Guid worklogId, UpdateWorklogCommand command)
    {
        if (worklogId != command.WorklogId)
            return BadRequest("Route worklogId does not match command worklogId.");
        await Mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{worklogId:guid}")]
    public async Task<IActionResult> Delete(Guid worklogId, [FromQuery] Guid projectId)
    {
        await Mediator.Send(new DeleteWorklogCommand(projectId, worklogId));
        return NoContent();
    }
}
