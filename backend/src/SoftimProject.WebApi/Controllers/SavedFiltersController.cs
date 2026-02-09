using Microsoft.AspNetCore.Mvc;
using SoftimProject.Application.Features.SavedFilters;

namespace SoftimProject.WebApi.Controllers;

[Route("api/v{version:apiVersion}/saved-filters")]
public class SavedFiltersController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<SavedFilterDto>>> GetAll(
        [FromQuery] Guid? projectId = null,
        [FromQuery] string viewType = "")
    {
        var result = await Mediator.Send(new GetSavedFiltersQuery(projectId, viewType));
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateSavedFilterCommand command)
    {
        var id = await Mediator.Send(command);
        return Ok(id);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, UpdateSavedFilterCommand command)
    {
        if (id != command.Id) return BadRequest();
        await Mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        await Mediator.Send(new DeleteSavedFilterCommand(id));
        return NoContent();
    }
}
