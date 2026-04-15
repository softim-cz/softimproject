using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Application.Features.Projects.CustomFields;

// DTO
public sealed record ProjectCustomFieldValueDto(
    Guid CustomFieldDefinitionId,
    string FieldName,
    string FieldType,
    string? Description,
    bool IsRequired,
    string? Options,
    string? Value);

// GET - returns all definitions with values (null if not filled)
public sealed record GetProjectCustomFieldValuesQuery(Guid ProjectId) : IRequest<List<ProjectCustomFieldValueDto>>;

public sealed class GetProjectCustomFieldValuesQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetProjectCustomFieldValuesQuery, List<ProjectCustomFieldValueDto>>
{
    public async Task<List<ProjectCustomFieldValueDto>> Handle(GetProjectCustomFieldValuesQuery request, CancellationToken cancellationToken)
    {
        var definitions = await dbContext.CustomFieldDefinitions
            .Where(d => d.IsActive)
            .OrderBy(d => d.SortOrder).ThenBy(d => d.Name)
            .ToListAsync(cancellationToken);

        var values = await dbContext.ProjectCustomFieldValues
            .Where(v => v.ProjectId == request.ProjectId)
            .ToListAsync(cancellationToken);

        var valueLookup = values.ToDictionary(v => v.CustomFieldDefinitionId, v => v.Value);

        return definitions.Select(d => new ProjectCustomFieldValueDto(
            d.Id,
            d.Name,
            d.FieldType.ToString(),
            d.Description,
            d.IsRequired,
            d.Options,
            valueLookup.GetValueOrDefault(d.Id)
        )).ToList();
    }
}

// SAVE (upsert)
public sealed record FieldValueInput(Guid CustomFieldDefinitionId, string? Value);

public sealed record SaveProjectCustomFieldValuesCommand(
    Guid ProjectId,
    List<FieldValueInput> Values) : IRequest;

public sealed class SaveProjectCustomFieldValuesCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<SaveProjectCustomFieldValuesCommand>
{
    public async Task Handle(SaveProjectCustomFieldValuesCommand request, CancellationToken cancellationToken)
    {
        var existing = await dbContext.ProjectCustomFieldValues
            .Where(v => v.ProjectId == request.ProjectId)
            .ToListAsync(cancellationToken);

        var existingLookup = existing.ToDictionary(v => v.CustomFieldDefinitionId);

        foreach (var input in request.Values)
        {
            if (existingLookup.TryGetValue(input.CustomFieldDefinitionId, out var value))
            {
                value.Value = input.Value;
            }
            else
            {
                dbContext.ProjectCustomFieldValues.Add(new ProjectCustomFieldValue
                {
                    Id = Guid.NewGuid(),
                    ProjectId = request.ProjectId,
                    CustomFieldDefinitionId = input.CustomFieldDefinitionId,
                    Value = input.Value,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
