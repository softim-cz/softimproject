using Microsoft.AspNetCore.Mvc;
using SoftimProject.Application.Features.Migration;
using SoftimProject.Application.Features.Migration.EasyProject;
using SoftimProject.Application.Features.Migration.EasyProject.Dtos;

namespace SoftimProject.WebApi.Controllers;

public class MigrationController : ApiControllerBase
{
    [HttpPost("normalize-html")]
    public async Task<ActionResult<NormalizeHtmlContentResult>> NormalizeHtml()
    {
        return Ok(await Mediator.Send(new NormalizeHtmlContentCommand()));
    }

    [HttpPost("test-connection")]
    public async Task<ActionResult<TestEpConnectionResult>> TestConnection([FromBody] TestEpConnectionQuery query)
    {
        var result = await Mediator.Send(query);
        return Ok(result);
    }

    [HttpPost("fetch-projects")]
    public async Task<ActionResult<List<EpProjectPreviewDto>>> FetchProjects([FromBody] FetchEpProjectsQuery query)
    {
        return Ok(await Mediator.Send(query));
    }

    [HttpPost("fetch-issue-counts")]
    public async Task<IActionResult> FetchIssueCounts([FromBody] FetchIssueCountsCommand command)
    {
        await Mediator.Send(command);
        return Ok();
    }

    [HttpPost("fetch-lookups")]
    public async Task<ActionResult<FetchEpLookupsResult>> FetchLookups([FromBody] FetchEpLookupsQuery query)
    {
        return Ok(await Mediator.Send(query));
    }

    [HttpPost("fetch-users")]
    public async Task<ActionResult<List<EpUserMappingDto>>> FetchUsers([FromBody] FetchEpUsersQuery query)
    {
        return Ok(await Mediator.Send(query));
    }

    [HttpPost("start")]
    public async Task<ActionResult<Guid>> Start([FromBody] StartMigrationCommand command)
    {
        var jobId = await Mediator.Send(command);
        return Ok(jobId);
    }

    [HttpPost("validate")]
    public async Task<ActionResult<MigrationValidationResult>> Validate([FromBody] ValidateMigrationQuery query)
    {
        return Ok(await Mediator.Send(query));
    }

    [HttpPost("{jobId:guid}/resume")]
    public async Task<ActionResult<Guid>> Resume(Guid jobId, [FromBody] ResumeMigrationBody body)
    {
        var id = await Mediator.Send(new ResumeMigrationCommand(jobId, body.ApiKey));
        return Ok(id);
    }

    public sealed record ResumeMigrationBody(string ApiKey);

    [HttpPost("{jobId:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid jobId)
    {
        await Mediator.Send(new CancelMigrationCommand(jobId));
        return NoContent();
    }

    [HttpGet("{jobId:guid}/progress")]
    public async Task<ActionResult<MigrationProgressDto>> GetProgress(Guid jobId)
    {
        var progress = await Mediator.Send(new GetMigrationProgressQuery(jobId));
        return progress != null ? Ok(progress) : NotFound();
    }

    [HttpGet("history")]
    public async Task<ActionResult<List<MigrationJobDto>>> GetHistory()
    {
        return Ok(await Mediator.Send(new GetMigrationHistoryQuery()));
    }
}
