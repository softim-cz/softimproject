using Microsoft.AspNetCore.Mvc;
using SoftimProject.Application.Features.Projects.ClientPortal;
using SoftimProject.Application.Features.Projects.CreateProject;
using SoftimProject.Application.Features.Projects.CustomFields;
using SoftimProject.Application.Features.Projects.DeleteProject;
using SoftimProject.Application.Features.Projects.GetProjectByCode;
using SoftimProject.Application.Features.Projects.GetProjectById;
using SoftimProject.Application.Features.Projects.GetProjects;
using SoftimProject.Application.Features.Projects.GitHub;
using SoftimProject.Application.Features.Projects.Members.AddProjectMember;
using SoftimProject.Application.Features.Projects.Members.GetUsers;
using SoftimProject.Application.Features.Projects.Members.RemoveProjectMember;
using SoftimProject.Application.Features.Projects.Members.UpdateProjectMember;
using SoftimProject.Application.Features.Projects.UpdateProject;
using SoftimProject.Application.Features.Projects;

namespace SoftimProject.WebApi.Controllers;

public class ProjectsController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ProjectDto>>> GetAll()
    {
        return Ok(await Mediator.Send(new GetProjectsQuery()));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProjectDetailDto>> GetById(Guid id)
    {
        return Ok(await Mediator.Send(new GetProjectByIdQuery(id)));
    }

    [HttpGet("by-code/{code}")]
    public async Task<ActionResult<ProjectDetailDto>> GetByCode(string code)
    {
        return Ok(await Mediator.Send(new GetProjectByCodeQuery(code)));
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateProjectCommand command)
    {
        var id = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateProjectCommand command)
    {
        if (id != command.Id) return BadRequest("Route id does not match command id.");
        await Mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await Mediator.Send(new DeleteProjectCommand(id));
        return NoContent();
    }

    // --- Members ---

    [HttpGet("users")]
    public async Task<ActionResult<List<UserOptionDto>>> GetUsers()
    {
        return Ok(await Mediator.Send(new GetUsersQuery()));
    }

    [HttpPost("{projectId:guid}/members")]
    public async Task<IActionResult> AddMember(Guid projectId, AddProjectMemberCommand command)
    {
        if (projectId != command.ProjectId) return BadRequest("Route projectId does not match command projectId.");
        var id = await Mediator.Send(command);
        return Ok(id);
    }

    [HttpPut("{projectId:guid}/members/{memberId:guid}")]
    public async Task<IActionResult> UpdateMember(Guid projectId, Guid memberId, UpdateProjectMemberCommand command)
    {
        if (projectId != command.ProjectId || memberId != command.MemberId)
            return BadRequest("Route ids do not match command ids.");
        await Mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{projectId:guid}/members/{memberId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid projectId, Guid memberId)
    {
        await Mediator.Send(new RemoveProjectMemberCommand(projectId, memberId));
        return NoContent();
    }

    // --- Custom Fields ---

    [HttpGet("{projectId:guid}/custom-fields")]
    public async Task<ActionResult<List<ProjectCustomFieldValueDto>>> GetCustomFields(Guid projectId)
    {
        return Ok(await Mediator.Send(new GetProjectCustomFieldValuesQuery(projectId)));
    }

    [HttpPut("{projectId:guid}/custom-fields")]
    public async Task<IActionResult> SaveCustomFields(Guid projectId, SaveProjectCustomFieldValuesCommand command)
    {
        if (projectId != command.ProjectId) return BadRequest("Route projectId does not match command projectId.");
        await Mediator.Send(command);
        return NoContent();
    }

    // --- GitHub Integration ---

    [HttpPost("{projectId:guid}/github/link")]
    public async Task<IActionResult> LinkGitHubRepo(Guid projectId, LinkGitHubRepoCommand command)
    {
        if (projectId != command.ProjectId) return BadRequest("Route projectId does not match command projectId.");
        await Mediator.Send(command);
        return NoContent();
    }

    [HttpPost("{projectId:guid}/github/unlink")]
    public async Task<IActionResult> UnlinkGitHubRepo(Guid projectId)
    {
        await Mediator.Send(new UnlinkGitHubRepoCommand(projectId));
        return NoContent();
    }

    [HttpPost("{projectId:guid}/github/test")]
    public async Task<ActionResult<TestGitHubConnectionResult>> TestGitHubConnection(Guid projectId)
    {
        var result = await Mediator.Send(new TestGitHubConnectionQuery(projectId));
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{projectId:guid}/github/sync")]
    public async Task<ActionResult<TriggerGitHubSyncResult>> TriggerGitHubSync(Guid projectId)
    {
        var result = await Mediator.Send(new TriggerGitHubSyncCommand(projectId));
        return result.Error == null ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{projectId:guid}/portal/token")]
    public async Task<ActionResult<string>> GenerateClientAccessToken(Guid projectId)
    {
        var token = await Mediator.Send(new GenerateClientAccessTokenCommand(projectId));
        return Ok(new { token });
    }

    [HttpPost("{projectId:guid}/portal/revoke")]
    public async Task<IActionResult> RevokeClientAccess(Guid projectId)
    {
        await Mediator.Send(new RevokeClientAccessCommand(projectId));
        return NoContent();
    }
}


