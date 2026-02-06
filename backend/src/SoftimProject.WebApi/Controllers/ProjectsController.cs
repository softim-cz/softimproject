using Microsoft.AspNetCore.Mvc;
using SoftimProject.Application.Features.Projects.CreateProject;
using SoftimProject.Application.Features.Projects.DeleteProject;
using SoftimProject.Application.Features.Projects.GetProjectById;
using SoftimProject.Application.Features.Projects.GetProjects;
using SoftimProject.Application.Features.Projects.Members.AddProjectMember;
using SoftimProject.Application.Features.Projects.Members.RemoveProjectMember;
using SoftimProject.Application.Features.Projects.Members.UpdateProjectMember;
using SoftimProject.Application.Features.Projects.UpdateProject;

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
}
