using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Application.Features.Lookups.ProjectTemplates;

public sealed record DuplicateProjectTemplateCommand(Guid SourceTemplateId, string NewName) : IRequest<Guid>, IRequireRole
{
    public string RequiredRole => "Admin";
}

public sealed class DuplicateProjectTemplateCommandValidator : AbstractValidator<DuplicateProjectTemplateCommand>
{
    public DuplicateProjectTemplateCommandValidator()
    {
        RuleFor(x => x.SourceTemplateId).NotEmpty();
        RuleFor(x => x.NewName).NotEmpty().MaximumLength(200);
    }
}

public sealed class DuplicateProjectTemplateCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<DuplicateProjectTemplateCommand, Guid>
{
    public async Task<Guid> Handle(DuplicateProjectTemplateCommand request, CancellationToken cancellationToken)
    {
        var source = await dbContext.ProjectTemplates
            .Include(t => t.Fields)
            .Include(t => t.TaskStates)
            .Include(t => t.TicketPriorities)
            .Include(t => t.AllowedTaskTypes)
            .FirstOrDefaultAsync(t => t.Id == request.SourceTemplateId, cancellationToken)
            ?? throw new NotFoundException(nameof(ProjectTemplate), request.SourceTemplateId);

        var newTemplate = new ProjectTemplate
        {
            Id = Guid.NewGuid(),
            Name = request.NewName,
            Description = source.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Zkopírovat allow-list typů úkolů (M:N — sdílíme stejné TaskType entity).
        foreach (var taskType in source.AllowedTaskTypes)
            newTemplate.AllowedTaskTypes.Add(taskType);

        dbContext.ProjectTemplates.Add(newTemplate);

        // Copy fields
        foreach (var field in source.Fields)
        {
            dbContext.ProjectTemplateFields.Add(new ProjectTemplateField
            {
                Id = Guid.NewGuid(),
                ProjectTemplateId = newTemplate.Id,
                CustomFieldDefinitionId = field.CustomFieldDefinitionId,
                SortOrder = field.SortOrder,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Copy task states
        foreach (var ts in source.TaskStates)
        {
            dbContext.TaskStates.Add(new TaskState
            {
                Id = Guid.NewGuid(),
                ProjectTemplateId = newTemplate.Id,
                Name = ts.Name,
                Color = ts.Color,
                SortOrder = ts.SortOrder,
                IsActive = ts.IsActive,
                IsDefault = ts.IsDefault,
                IsClosedState = ts.IsClosedState,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Copy ticket priorities
        foreach (var tp in source.TicketPriorities)
        {
            dbContext.TicketPriorities.Add(new TicketPriority
            {
                Id = Guid.NewGuid(),
                ProjectTemplateId = newTemplate.Id,
                Name = tp.Name,
                Color = tp.Color,
                SortOrder = tp.SortOrder,
                IsActive = tp.IsActive,
                IsDefault = tp.IsDefault,
                CreatedAt = DateTime.UtcNow
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return newTemplate.Id;
    }
}
