using Microsoft.AspNetCore.Mvc;
using SoftimProject.Application.Features.Admin;

namespace SoftimProject.WebApi.Controllers;

[Route("api/v{version:apiVersion}/admin")]
public class AdminController : ApiControllerBase
{
    [HttpGet("users")]
    public async Task<ActionResult<List<AdminUserDto>>> GetUsers()
        => Ok(await Mediator.Send(new GetAdminUsersQuery()));

    [HttpPut("users/{userId:guid}/roles")]
    public async Task<IActionResult> UpdateUserRoles(Guid userId, UpdateUserRolesCommand command)
    {
        if (userId != command.UserId) return BadRequest("Route userId does not match command userId.");
        await Mediator.Send(command);
        return NoContent();
    }
}
