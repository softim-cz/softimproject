using Microsoft.AspNetCore.Mvc;
using SoftimProject.Application.Features.Worklogs.AiHistory;
using SoftimProject.Application.Features.Worklogs.CreateWorklog;
using SoftimProject.Application.Features.Worklogs.CreateWorklogsBatch;
using SoftimProject.Application.Features.Worklogs.DeleteWorklog;
using SoftimProject.Application.Features.Worklogs.GetWorklogById;
using SoftimProject.Application.Features.Worklogs.GetWorklogs;
using SoftimProject.Application.Features.Worklogs.ImportWorklogs;
using SoftimProject.Application.Features.Worklogs.UpdateWorklog;

namespace SoftimProject.WebApi.Controllers;

public class WorklogsController : ApiControllerBase
{
    /// <summary>Lists worklogs (paged) filtered by project, ticket, user, date range; optionally includes sub-projects.</summary>
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

    /// <summary>Logs work on a ticket (date, hours, description, billable).</summary>
    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateWorklogCommand command)
    {
        var id = await Mediator.Send(command);
        return Ok(id);
    }

    /// <summary>Logs several worklog entries against one ticket in a single transaction (bulk entry).</summary>
    [HttpPost("batch")]
    public async Task<ActionResult<IReadOnlyList<Guid>>> CreateBatch(CreateWorklogsBatchCommand command)
    {
        var ids = await Mediator.Send(command);
        return Ok(ids);
    }

    /// <summary>Imports worklogs from an .xlsx or .csv file; reports created, skipped duplicates and errors.</summary>
    [HttpPost("import")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ImportWorklogsResult>> Import(
        [FromForm] Guid projectId,
        IFormFile file,
        [FromForm] Guid? overrideUserId = null,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file uploaded.");

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, cancellationToken);

        var result = await Mediator.Send(
            new ImportWorklogsCommand(projectId, file.FileName, ms.ToArray(), overrideUserId));
        return Ok(result);
    }

    /// <summary>Updates a worklog entry.</summary>
    [HttpPut("{worklogId:guid}")]
    public async Task<IActionResult> Update(Guid worklogId, UpdateWorklogCommand command)
    {
        if (worklogId != command.WorklogId)
            return BadRequest("Route worklogId does not match command worklogId.");
        await Mediator.Send(command);
        return NoContent();
    }

    /// <summary>Deletes a worklog entry.</summary>
    [HttpDelete("{worklogId:guid}")]
    public async Task<IActionResult> Delete(Guid worklogId, [FromQuery] Guid projectId)
    {
        await Mediator.Send(new DeleteWorklogCommand(projectId, worklogId));
        return NoContent();
    }

    /// <summary>Generates (or regenerates) the AI summary for a worklog entry.</summary>
    [HttpPost("{worklogId:guid}/ai/resummarize")]
    public async Task<ActionResult<object>> Resummarize(Guid worklogId, [FromBody] ResummarizeWorklogBody body)
    {
        var id = await Mediator.Send(new ResummarizeWorklogCommand(body.ProjectId, worklogId));
        return Ok(new { invocationId = id });
    }

    public sealed record ResummarizeWorklogBody(Guid ProjectId);
}
