using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoftimProject.Application.Features.Health;

namespace SoftimProject.WebApi.Controllers;

// Deliberately AllowAnonymous so container probes / infra monitoring can reach the
// endpoint without provisioning credentials. The payload doesn't leak sensitive data
// (just job names, timestamps, last-run counters).
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[AllowAnonymous]
public sealed class HealthController(ISender mediator) : ControllerBase
{
    [HttpGet("jobs")]
    [ProducesResponseType(typeof(JobsHealthDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(JobsHealthDto), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetJobs(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetJobsHealthQuery(), cancellationToken);
        return result.Status == "Healthy"
            ? Ok(result)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, result);
    }
}
