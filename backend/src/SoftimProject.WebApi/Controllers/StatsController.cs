using Microsoft.AspNetCore.Mvc;
using SoftimProject.Application.Features.Dashboard;

namespace SoftimProject.WebApi.Controllers;

[Route("api/v{version:apiVersion}/stats")]
public class StatsController : ApiControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardStatsDto>> GetDashboard()
    {
        return Ok(await Mediator.Send(new GetDashboardStatsQuery()));
    }
}
