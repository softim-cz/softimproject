using Microsoft.AspNetCore.Mvc;
using SoftimProject.Application.Features.Auth;

namespace SoftimProject.WebApi.Controllers;

[Route("api/v{version:apiVersion}/me")]
public class AuthController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CurrentUserDto>> GetMe()
    {
        return Ok(await Mediator.Send(new GetCurrentUserQuery()));
    }
}
