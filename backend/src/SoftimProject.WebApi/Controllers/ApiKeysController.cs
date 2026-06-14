using Microsoft.AspNetCore.Mvc;
using SoftimProject.Application.Features.ApiKeys;

namespace SoftimProject.WebApi.Controllers;

[Route("api/v{version:apiVersion}/api-keys")]
public class ApiKeysController : ApiControllerBase
{
    /// <summary>Lists the current user's personal API keys (secrets are never returned).</summary>
    [HttpGet]
    public async Task<ActionResult<List<ApiKeyDto>>> GetAll()
    {
        return Ok(await Mediator.Send(new GetApiKeysQuery()));
    }

    /// <summary>Generates a new personal API key. The plaintext key is returned ONCE here.</summary>
    [HttpPost]
    public async Task<ActionResult<GenerateApiKeyResult>> Generate(GenerateApiKeyCommand command)
    {
        return Ok(await Mediator.Send(command));
    }

    /// <summary>Revokes an API key (own key, or any key for Admins).</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Revoke(Guid id)
    {
        await Mediator.Send(new RevokeApiKeyCommand(id));
        return NoContent();
    }
}
