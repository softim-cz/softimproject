using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Projects.CreateProject;

public sealed record CreateProjectCommand(
    string Name,
    string? Code,
    string? Description,
    decimal? BudgetHours,
    decimal? BudgetAmount,
    DateOnly? StartDate,
    DateOnly? EndDate,
    DateOnly? DeadlineDate,
    Guid ProjectTemplateId,
    Guid? CompanyId = null,
    Guid? ProjectTypeId = null,
    Guid? ProjectStateId = null,
    Guid? ParentProjectId = null) : IRequest<Guid>, IRequireRole
{
    public string RequiredRole => "Admin";
}

public sealed class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator(IApplicationDbContext dbContext)
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).MinimumLength(2).MaximumLength(6)
            .Matches("^[A-Z0-9]+$").WithMessage("Code must be uppercase alphanumeric.")
            .When(x => !string.IsNullOrEmpty(x.Code));
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.BudgetHours).GreaterThan(0).When(x => x.BudgetHours.HasValue);
        RuleFor(x => x.BudgetAmount).GreaterThan(0).When(x => x.BudgetAmount.HasValue);
        RuleFor(x => x.ProjectTemplateId)
            .NotEmpty()
            .WithMessage("Šablona projektu je povinná.");
        RuleFor(x => x.ProjectTemplateId)
            .MustAsync(async (id, ct) =>
                await dbContext.ProjectTemplates.AnyAsync(t => t.Id == id && t.IsActive, ct))
            .WithMessage("Šablona projektu neexistuje nebo není aktivní.");
    }
}

public sealed class CreateProjectCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService) : IRequestHandler<CreateProjectCommand, Guid>
{
    public async Task<Guid> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        var code = string.IsNullOrWhiteSpace(request.Code)
            ? GenerateCode(request.Name)
            : request.Code;

        code = await EnsureUniqueCode(code, cancellationToken);

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Code = code,
            Description = request.Description,
            Status = ProjectStatus.Active,
            BudgetHours = request.BudgetHours,
            BudgetAmount = request.BudgetAmount,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            DeadlineDate = request.DeadlineDate,
            CompanyId = request.CompanyId,
            ProjectTypeId = request.ProjectTypeId,
            ProjectStateId = request.ProjectStateId,
            ParentProjectId = request.ParentProjectId,
            ProjectTemplateId = request.ProjectTemplateId,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Projects.Add(project);

        // Seed custom field values from template
        var templateFields = await dbContext.ProjectTemplateFields
            .Where(f => f.ProjectTemplateId == request.ProjectTemplateId)
            .ToListAsync(cancellationToken);

        foreach (var field in templateFields)
        {
            dbContext.ProjectCustomFieldValues.Add(new ProjectCustomFieldValue
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                CustomFieldDefinitionId = field.CustomFieldDefinitionId,
                Value = null,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Add creator as ProjectManager
        if (currentUserService.UserId.HasValue)
        {
            var member = new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                UserId = currentUserService.UserId.Value,
                Role = ProjectRole.ProjectManager,
                JoinedAt = DateTime.UtcNow
            };
            dbContext.ProjectMembers.Add(member);
        }

        // Create default kanban board
        var board = new KanbanBoard
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Name = "Main Board",
            IsDefault = true,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.KanbanBoards.Add(board);

        // Create default columns from active TaskStates of the project's template
        // — bez scopu by sloupce ukazovaly na stavy jiných šablon a projekt by
        // viděl stavy, které mu nepatří.
        var taskStates = await dbContext.TaskStates
            .Where(ts => ts.IsActive && ts.ProjectTemplateId == request.ProjectTemplateId)
            .OrderBy(ts => ts.SortOrder)
            .ToListAsync(cancellationToken);

        for (var i = 0; i < taskStates.Count; i++)
        {
            var column = new KanbanColumn
            {
                Id = Guid.NewGuid(),
                BoardId = board.Id,
                Name = taskStates[i].Name,
                Position = i,
                CreatedAt = DateTime.UtcNow
            };
            column.MapsToTaskStates.Add(taskStates[i]);
            dbContext.KanbanColumns.Add(column);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return project.Id;
    }

    private static string GenerateCode(string name)
    {
        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (words.Length == 1)
        {
            // Single word: take first 3 chars
            return words[0][..Math.Min(3, words[0].Length)].ToUpperInvariant();
        }

        // Multiple words: take first letter of each word, max 6
        var initials = string.Concat(words.Take(6).Select(w => char.ToUpperInvariant(w[0])));
        return initials;
    }

    private async Task<string> EnsureUniqueCode(string baseCode, CancellationToken cancellationToken)
    {
        if (!await dbContext.Projects.AnyAsync(p => p.Code == baseCode, cancellationToken))
            return baseCode;

        for (var i = 2; i <= 99; i++)
        {
            var candidate = $"{baseCode}{i}";
            if (candidate.Length > 6)
                candidate = $"{baseCode[..Math.Max(2, 6 - i.ToString().Length)]}{i}";

            if (!await dbContext.Projects.AnyAsync(p => p.Code == candidate, cancellationToken))
                return candidate;
        }

        // Fallback: should never happen in practice
        return $"{baseCode[..2]}{Guid.NewGuid().ToString()[..4].ToUpperInvariant()}";
    }
}
