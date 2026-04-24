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

    [HttpPut("users/{userId:guid}/global-role")]
    public async Task<IActionResult> UpdateUserGlobalRole(Guid userId, UpdateUserGlobalRoleCommand command)
    {
        if (userId != command.UserId) return BadRequest("Route userId does not match command userId.");
        await Mediator.Send(command);
        return NoContent();
    }

    [HttpPut("users/{userId:guid}/active")]
    public async Task<IActionResult> UpdateUserActive(Guid userId, UpdateUserActiveCommand command)
    {
        if (userId != command.UserId) return BadRequest("Route userId does not match command userId.");
        await Mediator.Send(command);
        return NoContent();
    }

    // --- Dead-letter queue ---

    [HttpGet("dead-letter")]
    public async Task<ActionResult<List<DeadLetterEntryDto>>> GetDeadLetter([FromQuery] bool includeResolved = false)
        => Ok(await Mediator.Send(new GetDeadLetterEntriesQuery(includeResolved)));

    [HttpPost("dead-letter/{id:guid}/replay")]
    public async Task<IActionResult> ReplayDeadLetter(Guid id)
    {
        await Mediator.Send(new ReplayDeadLetterCommand(id));
        return NoContent();
    }

    [HttpPost("dead-letter/{id:guid}/dismiss")]
    public async Task<IActionResult> DismissDeadLetter(Guid id)
    {
        await Mediator.Send(new DismissDeadLetterCommand(id));
        return NoContent();
    }

    // --- AI usage ---

    [HttpGet("ai-usage")]
    public async Task<ActionResult<AiUsageDto>> GetAiUsage([FromQuery] int days = 30)
        => Ok(await Mediator.Send(new GetAiUsageQuery(days)));
}
