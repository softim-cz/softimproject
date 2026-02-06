using FluentValidation;
using MediatR;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Projects.CreateProject;

public sealed record CreateProjectCommand(
    string Name,
    string Code,
    string? Description,
    decimal? BudgetHours,
    decimal? BudgetAmount,
    DateOnly? StartDate,
    DateOnly? EndDate,
    DateOnly? DeadlineDate) : IRequest<Guid>;

public sealed class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MinimumLength(2).MaximumLength(6)
            .Matches("^[A-Z]+$").WithMessage("Code must be uppercase letters only.");
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.BudgetHours).GreaterThan(0).When(x => x.BudgetHours.HasValue);
        RuleFor(x => x.BudgetAmount).GreaterThan(0).When(x => x.BudgetAmount.HasValue);
    }
}

public sealed class CreateProjectCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService) : IRequestHandler<CreateProjectCommand, Guid>
{
    public async Task<Guid> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Code = request.Code,
            Description = request.Description,
            Status = ProjectStatus.Active,
            BudgetHours = request.BudgetHours,
            BudgetAmount = request.BudgetAmount,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            DeadlineDate = request.DeadlineDate,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Projects.Add(project);

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

        // Create default columns
        var defaultColumns = new[]
        {
            (Name: "Backlog", Position: 0, Status: TicketStatus.Backlog),
            (Name: "To Do", Position: 1, Status: TicketStatus.Todo),
            (Name: "In Progress", Position: 2, Status: TicketStatus.InProgress),
            (Name: "Review", Position: 3, Status: TicketStatus.Review),
            (Name: "Done", Position: 4, Status: TicketStatus.Done)
        };

        foreach (var col in defaultColumns)
        {
            dbContext.KanbanColumns.Add(new KanbanColumn
            {
                Id = Guid.NewGuid(),
                BoardId = board.Id,
                Name = col.Name,
                Position = col.Position,
                MapsToStatus = col.Status,
                CreatedAt = DateTime.UtcNow
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return project.Id;
    }
}
