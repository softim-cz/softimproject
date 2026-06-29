using Microsoft.AspNetCore.Mvc;
using SoftimProject.Application.Features.Integration;

namespace SoftimProject.WebApi.Controllers;

// Provider-agnostic import/connection flow (#144 unification). Same endpoints serve
// EasyProject, Jira and Redmine — the SourceSystem in the body picks the connector.
[Route("api/v{version:apiVersion}/integration")]
public class IntegrationController : ApiControllerBase
{
    [HttpPost("test-connection")]
    public async Task<ActionResult<ConnectionTestResult>> TestConnection(SourceConnectionInput input)
        => Ok(await Mediator.Send(new TestSourceConnectionQuery(input)));

    /// <summary>Remembers the connection (system + URL + token) after a successful test, before any import.</summary>
    [HttpPost("remember-connection")]
    public async Task<ActionResult<Guid>> RememberConnection(SourceConnectionInput input)
        => Ok(await Mediator.Send(new RememberSourceConnectionCommand(input)));

    [HttpPost("projects")]
    public async Task<ActionResult<List<SourceProjectPreviewDto>>> FetchProjects(SourceConnectionInput input)
        => Ok(await Mediator.Send(new FetchSourceProjectsQuery(input)));

    [HttpPost("lookups")]
    public async Task<ActionResult<SourceLookupsResult>> FetchLookups(SourceConnectionInput input)
        => Ok(await Mediator.Send(new FetchSourceLookupsQuery(input)));

    [HttpPost("users")]
    public async Task<ActionResult<List<SourceUserMappingDto>>> FetchUsers(SourceConnectionInput input)
        => Ok(await Mediator.Send(new FetchSourceUsersQuery(input)));

    /// <summary>Starts a one-time import for the chosen system and creates/updates its connection.</summary>
    [HttpPost("start")]
    public async Task<ActionResult<Guid>> Start(StartSourceImportCommand command)
        => Ok(await Mediator.Send(command));
}
