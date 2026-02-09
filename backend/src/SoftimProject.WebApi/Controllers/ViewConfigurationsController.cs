using Microsoft.AspNetCore.Mvc;
using SoftimProject.Application.Features.ViewConfigurations;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.WebApi.Controllers;

[Route("api/v{version:apiVersion}/view-configurations")]
public class ViewConfigurationsController(ICurrentUserService currentUserService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ViewConfigurationDto?>> Get(
        [FromQuery] Guid? projectId = null,
        [FromQuery] string viewType = "")
    {
        var userId = currentUserService.UserId
                     ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var result = await Mediator.Send(new GetViewConfigurationQuery(userId, projectId, viewType));
        return Ok(result);
    }

    [HttpPut]
    public async Task<ActionResult<Guid>> Upsert(UpsertViewConfigurationCommand command)
    {
        var id = await Mediator.Send(command);
        return Ok(id);
    }
}
