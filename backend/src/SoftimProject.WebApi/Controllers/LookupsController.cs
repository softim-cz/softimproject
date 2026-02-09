using Microsoft.AspNetCore.Mvc;
using SoftimProject.Application.Features.Lookups.ApplicationRoles;
using SoftimProject.Application.Features.Lookups.Companies;
using SoftimProject.Application.Features.Lookups.ProjectStates;
using SoftimProject.Application.Features.Lookups.ProjectTypes;
using SoftimProject.Application.Features.Lookups.TaskStates;
using SoftimProject.Application.Features.Lookups.TaskTypes;

namespace SoftimProject.WebApi.Controllers;

[Route("api/v{version:apiVersion}/lookups")]
public class LookupsController : ApiControllerBase
{
    // === Companies ===

    [HttpGet("companies")]
    public async Task<ActionResult<List<CompanyDto>>> GetCompanies()
        => Ok(await Mediator.Send(new GetCompaniesQuery()));

    [HttpPost("companies")]
    public async Task<ActionResult<Guid>> CreateCompany(CreateCompanyCommand command)
        => CreatedAtAction(nameof(GetCompanies), await Mediator.Send(command));

    [HttpPut("companies/{id:guid}")]
    public async Task<IActionResult> UpdateCompany(Guid id, UpdateCompanyCommand command)
    {
        if (id != command.Id) return BadRequest("Route id does not match command id.");
        await Mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("companies/{id:guid}")]
    public async Task<IActionResult> DeleteCompany(Guid id)
    {
        await Mediator.Send(new DeleteCompanyCommand(id));
        return NoContent();
    }

    // === Project Types ===

    [HttpGet("project-types")]
    public async Task<ActionResult<List<ProjectTypeDto>>> GetProjectTypes()
        => Ok(await Mediator.Send(new GetProjectTypesQuery()));

    [HttpPost("project-types")]
    public async Task<ActionResult<Guid>> CreateProjectType(CreateProjectTypeCommand command)
        => CreatedAtAction(nameof(GetProjectTypes), await Mediator.Send(command));

    [HttpPut("project-types/{id:guid}")]
    public async Task<IActionResult> UpdateProjectType(Guid id, UpdateProjectTypeCommand command)
    {
        if (id != command.Id) return BadRequest("Route id does not match command id.");
        await Mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("project-types/{id:guid}")]
    public async Task<IActionResult> DeleteProjectType(Guid id)
    {
        await Mediator.Send(new DeleteProjectTypeCommand(id));
        return NoContent();
    }

    // === Project States ===

    [HttpGet("project-states")]
    public async Task<ActionResult<List<ProjectStateDto>>> GetProjectStates()
        => Ok(await Mediator.Send(new GetProjectStatesQuery()));

    [HttpPost("project-states")]
    public async Task<ActionResult<Guid>> CreateProjectState(CreateProjectStateCommand command)
        => CreatedAtAction(nameof(GetProjectStates), await Mediator.Send(command));

    [HttpPut("project-states/{id:guid}")]
    public async Task<IActionResult> UpdateProjectState(Guid id, UpdateProjectStateCommand command)
    {
        if (id != command.Id) return BadRequest("Route id does not match command id.");
        await Mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("project-states/{id:guid}")]
    public async Task<IActionResult> DeleteProjectState(Guid id)
    {
        await Mediator.Send(new DeleteProjectStateCommand(id));
        return NoContent();
    }

    // === Task Types ===

    [HttpGet("task-types")]
    public async Task<ActionResult<List<TaskTypeDto>>> GetTaskTypes()
        => Ok(await Mediator.Send(new GetTaskTypesQuery()));

    [HttpPost("task-types")]
    public async Task<ActionResult<Guid>> CreateTaskType(CreateTaskTypeCommand command)
        => CreatedAtAction(nameof(GetTaskTypes), await Mediator.Send(command));

    [HttpPut("task-types/{id:guid}")]
    public async Task<IActionResult> UpdateTaskType(Guid id, UpdateTaskTypeCommand command)
    {
        if (id != command.Id) return BadRequest("Route id does not match command id.");
        await Mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("task-types/{id:guid}")]
    public async Task<IActionResult> DeleteTaskType(Guid id)
    {
        await Mediator.Send(new DeleteTaskTypeCommand(id));
        return NoContent();
    }

    // === Task States ===

    [HttpGet("task-states")]
    public async Task<ActionResult<List<TaskStateDto>>> GetTaskStates()
        => Ok(await Mediator.Send(new GetTaskStatesQuery()));

    [HttpPost("task-states")]
    public async Task<ActionResult<Guid>> CreateTaskState(CreateTaskStateCommand command)
        => CreatedAtAction(nameof(GetTaskStates), await Mediator.Send(command));

    [HttpPut("task-states/{id:guid}")]
    public async Task<IActionResult> UpdateTaskState(Guid id, UpdateTaskStateCommand command)
    {
        if (id != command.Id) return BadRequest("Route id does not match command id.");
        await Mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("task-states/{id:guid}")]
    public async Task<IActionResult> DeleteTaskState(Guid id)
    {
        await Mediator.Send(new DeleteTaskStateCommand(id));
        return NoContent();
    }

    // === Application Roles ===

    [HttpGet("application-roles")]
    public async Task<ActionResult<List<ApplicationRoleDto>>> GetApplicationRoles()
        => Ok(await Mediator.Send(new GetApplicationRolesQuery()));

    [HttpPost("application-roles")]
    public async Task<ActionResult<Guid>> CreateApplicationRole(CreateApplicationRoleCommand command)
        => CreatedAtAction(nameof(GetApplicationRoles), await Mediator.Send(command));

    [HttpPut("application-roles/{id:guid}")]
    public async Task<IActionResult> UpdateApplicationRole(Guid id, UpdateApplicationRoleCommand command)
    {
        if (id != command.Id) return BadRequest("Route id does not match command id.");
        await Mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("application-roles/{id:guid}")]
    public async Task<IActionResult> DeleteApplicationRole(Guid id)
    {
        await Mediator.Send(new DeleteApplicationRoleCommand(id));
        return NoContent();
    }
}
