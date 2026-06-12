using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Features.Lookups.TaskTypes;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Projects.AllowedTaskTypes;

// DTO
// IsRestricted = project ticket creation is limited to EffectiveTaskTypes.
// OverrideTaskTypeIds = the project's own allow-list (empty = inherit template default).
// TemplateTaskTypeIds = the template default (shown so the UI can explain what is inherited).
// EffectiveTaskTypes = the resolved list actually offered when creating/editing tickets.
public sealed record ProjectAllowedTaskTypesDto(
    bool IsRestricted,
    List<Guid> OverrideTaskTypeIds,
    List<Guid> TemplateTaskTypeIds,
    List<TaskTypeDto> EffectiveTaskTypes);

// GET
public sealed record GetProjectAllowedTaskTypesQuery(Guid ProjectId)
    : IRequest<ProjectAllowedTaskTypesDto>, IRequireProjectAccess;

public sealed class GetProjectAllowedTaskTypesQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetProjectAllowedTaskTypesQuery, ProjectAllowedTaskTypesDto>
{
    public async Task<ProjectAllowedTaskTypesDto> Handle(GetProjectAllowedTaskTypesQuery request, CancellationToken cancellationToken)
    {
        var overrideIds = await dbContext.Projects
            .Where(p => p.Id == request.ProjectId)
            .SelectMany(p => p.AllowedTaskTypes.Select(tt => tt.Id))
            .ToListAsync(cancellationToken);

        var templateIds = await dbContext.Projects
            .Where(p => p.Id == request.ProjectId)
            .SelectMany(p => p.ProjectTemplate.AllowedTaskTypes.Select(tt => tt.Id))
            .ToListAsync(cancellationToken);

        var effectiveSet = overrideIds.Count > 0 ? overrideIds
            : templateIds.Count > 0 ? templateIds
            : null;

        var typesQuery = dbContext.TaskTypes.AsQueryable();
        if (effectiveSet is not null)
        {
            var set = effectiveSet.ToHashSet();
            typesQuery = typesQuery.Where(tt => set.Contains(tt.Id));
        }
        else
        {
            typesQuery = typesQuery.Where(tt => tt.IsActive);
        }

        var effective = await typesQuery
            .OrderBy(tt => tt.SortOrder).ThenBy(tt => tt.Name)
            .Select(tt => new TaskTypeDto(tt.Id, tt.Name, tt.NameCs, tt.NameEn, tt.Icon, tt.SortOrder, tt.IsActive))
            .ToListAsync(cancellationToken);

        return new ProjectAllowedTaskTypesDto(effectiveSet is not null, overrideIds, templateIds, effective);
    }
}

// SET (replace project override; empty list = inherit template default)
public sealed record SetProjectAllowedTaskTypesCommand(Guid ProjectId, List<Guid> TaskTypeIds)
    : IRequest, IRequireProjectRole
{
    public ProjectRole RequiredProjectRole => ProjectRole.ProjectManager;
}

public sealed class SetProjectAllowedTaskTypesCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<SetProjectAllowedTaskTypesCommand>
{
    public async Task Handle(SetProjectAllowedTaskTypesCommand request, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .Include(p => p.AllowedTaskTypes)
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Project), request.ProjectId);

        project.AllowedTaskTypes.Clear();

        if (request.TaskTypeIds.Count > 0)
        {
            var ids = request.TaskTypeIds.ToHashSet();
            var types = await dbContext.TaskTypes
                .Where(tt => ids.Contains(tt.Id))
                .ToListAsync(cancellationToken);

            foreach (var type in types)
                project.AllowedTaskTypes.Add(type);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
