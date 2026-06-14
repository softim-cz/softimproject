using Microsoft.AspNetCore.Mvc;
using SoftimProject.Application.Features.Worklogs.CreateWorklog;
using SoftimProject.Application.Features.Worklogs.DeleteWorklog;
using SoftimProject.Application.Features.Worklogs.GetWorklogById;
using SoftimProject.Application.Features.Worklogs.GetWorklogs;
using SoftimProject.Application.Features.Worklogs.UpdateWorklog;

namespace SoftimProject.WebApi.Controllers;

public class WorklogsController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? projectId = null,
        [FromQuery] Guid? ticketId = null,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] bool includeSubprojects = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        return Ok(await Mediator.Send(new GetWorklogsQuery(projectId, ticketId, from, to, userId, includeSubprojects, page, pageSize)));
    }

    [HttpGet("{worklogId:guid}")]
    public async Task<ActionResult<WorklogDto>> GetById(Guid worklogId)
    {
        return Ok(await Mediator.Send(new GetWorklogByIdQuery(worklogId)));
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
