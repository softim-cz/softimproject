using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Application.Features.Lookups.ProjectTemplates;

// DTOs
public sealed record ProjectTemplateFieldDto(Guid CustomFieldDefinitionId, string CustomFieldName, int SortOrder);

public sealed record TemplateTaskStateDto(Guid Id, string Name, string Color, int SortOrder, bool IsActive, bool IsDefault, bool IsClosedState);

public sealed record TemplateTicketPriorityDto(Guid Id, string Name, string Color, int SortOrder, bool IsActive, bool IsDefault);

public sealed record ProjectTemplateDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    List<ProjectTemplateFieldDto> Fields,
    List<TemplateTaskStateDto> TaskStates,
    List<TemplateTicketPriorityDto> TicketPriorities,
    List<Guid> AllowedTaskTypeIds);

// GET ALL
public sealed record GetProjectTemplatesQuery : IRequest<List<ProjectTemplateDto>>;

public sealed class GetProjectTemplatesQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetProjectTemplatesQuery, List<ProjectTemplateDto>>
{
    public async Task<List<ProjectTemplateDto>> Handle(GetProjectTemplatesQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.ProjectTemplates
            .Include(t => t.Fields).ThenInclude(f => f.CustomFieldDefinition)
            .Include(t => t.TaskStates)
            .Include(t => t.TicketPriorities)
            .Include(t => t.AllowedTaskTypes)
            .OrderBy(t => t.Name)
            .Select(t => new ProjectTemplateDto(
                t.Id, t.Name, t.Description, t.IsActive,
                t.Fields.OrderBy(f => f.SortOrder)
                    .Select(f => new ProjectTemplateFieldDto(
                        f.CustomFieldDefinitionId, f.CustomFieldDefinition.Name, f.SortOrder))
                    .ToList(),
                t.TaskStates.OrderBy(ts => ts.SortOrder).ThenBy(ts => ts.Name)
                    .Select(ts => new TemplateTaskStateDto(ts.Id, ts.Name, ts.Color, ts.SortOrder, ts.IsActive, ts.IsDefault, ts.IsClosedState))
                    .ToList(),
                t.TicketPriorities.OrderBy(tp => tp.SortOrder).ThenBy(tp => tp.Name)
                    .Select(tp => new TemplateTicketPriorityDto(tp.Id, tp.Name, tp.Color, tp.SortOrder, tp.IsActive, tp.IsDefault))
                    .ToList(),
                t.AllowedTaskTypes.Select(tt => tt.Id).ToList()))
            .ToListAsync(cancellationToken);
    }
}

// CREATE
public sealed record CreateProjectTemplateCommand(
    string Name,
    string? Description,
    List<Guid> CustomFieldDefinitionIds,
    List<Guid>? AllowedTaskTypeIds = null) : IRequest<Guid>, IRequireRole
{
    public string RequiredRole => "Admin";
}

public sealed class CreateProjectTemplateCommandValidator : AbstractValidator<CreateProjectTemplateCommand>
{
    public CreateProjectTemplateCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}

public sealed class CreateProjectTemplateCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<CreateProjectTemplateCommand, Guid>
{
    public async Task<Guid> Handle(CreateProjectTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = new ProjectTemplate
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.ProjectTemplates.Add(template);

        for (var i = 0; i < request.CustomFieldDefinitionIds.Count; i++)
        {
            dbContext.ProjectTemplateFields.Add(new ProjectTemplateField
            {
                Id = Guid.NewGuid(),
                ProjectTemplateId = template.Id,
                CustomFieldDefinitionId = request.CustomFieldDefinitionIds[i],
                SortOrder = (i + 1) * 10,
                CreatedAt = DateTime.UtcNow
            });
        }

        await ProjectTemplateTaskTypeHelper.AssignAllowedTaskTypesAsync(dbContext, template, request.AllowedTaskTypeIds, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        return template.Id;
    }
}

// UPDATE
public sealed record UpdateProjectTemplateCommand(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    List<Guid> CustomFieldDefinitionIds,
    List<Guid>? AllowedTaskTypeIds = null) : IRequest, IRequireRole
{
    public string RequiredRole => "Admin";
}

public sealed class UpdateProjectTemplateCommandValidator : AbstractValidator<UpdateProjectTemplateCommand>
{
    public UpdateProjectTemplateCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}

public sealed class UpdateProjectTemplateCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpdateProjectTemplateCommand>
{
    public async Task Handle(UpdateProjectTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await dbContext.ProjectTemplates
            .Include(t => t.Fields)
            .Include(t => t.AllowedTaskTypes)
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ProjectTemplate), request.Id);

        template.Name = request.Name;
        template.Description = request.Description;
        template.IsActive = request.IsActive;

        // Remove old fields
        dbContext.ProjectTemplateFields.RemoveRange(template.Fields);

        // Add new fields
        for (var i = 0; i < request.CustomFieldDefinitionIds.Count; i++)
        {
            dbContext.ProjectTemplateFields.Add(new ProjectTemplateField
            {
                Id = Guid.NewGuid(),
                ProjectTemplateId = template.Id,
                CustomFieldDefinitionId = request.CustomFieldDefinitionIds[i],
                SortOrder = (i + 1) * 10,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Null = ponechat beze změny; prázdný seznam = vymazat allow-list.
        if (request.AllowedTaskTypeIds is not null)
        {
            template.AllowedTaskTypes.Clear();
            await ProjectTemplateTaskTypeHelper.AssignAllowedTaskTypesAsync(dbContext, template, request.AllowedTaskTypeIds, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

// Shared helper for assigning the M:N allow-list onto a tracked template.
internal static class ProjectTemplateTaskTypeHelper
{
    public static async Task AssignAllowedTaskTypesAsync(
        IApplicationDbContext dbContext, ProjectTemplate template, List<Guid>? taskTypeIds, CancellationToken cancellationToken)
    {
        if (taskTypeIds is null || taskTypeIds.Count == 0)
            return;

        var ids = taskTypeIds.ToHashSet();
        var types = await dbContext.TaskTypes
            .Where(tt => ids.Contains(tt.Id))
            .ToListAsync(cancellationToken);

        foreach (var type in types)
            template.AllowedTaskTypes.Add(type);
    }
}

// DELETE
public sealed record DeleteProjectTemplateCommand(Guid Id) : IRequest, IRequireRole
{
    public string RequiredRole => "Admin";
}

public sealed class DeleteProjectTemplateCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<DeleteProjectTemplateCommand>
{
    public async Task Handle(DeleteProjectTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await dbContext.ProjectTemplates
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ProjectTemplate), request.Id);

        dbContext.ProjectTemplates.Remove(template);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
