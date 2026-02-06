using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Projects.UpdateProject;

public sealed record UpdateProjectCommand(
    Guid Id,
    string Name,
    string? Description,
    ProjectStatus Status,
    decimal? BudgetHours,
    decimal? BudgetAmount,
    DateOnly? StartDate,
    DateOnly? EndDate,
    DateOnly? DeadlineDate) : IRequest, IRequireProjectAccess
{
    public Guid ProjectId => Id;
}

public sealed class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.BudgetHours).GreaterThan(0).When(x => x.BudgetHours.HasValue);
        RuleFor(x => x.BudgetAmount).GreaterThan(0).When(x => x.BudgetAmount.HasValue);
    }
}

public sealed class UpdateProjectCommandHandler(
    IApplicationDbContext dbContext) : IRequestHandler<UpdateProjectCommand>
{
    public async Task Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Project), request.Id);

        project.Name = request.Name;
        project.Description = request.Description;
        project.Status = request.Status;
        project.BudgetHours = request.BudgetHours;
        project.BudgetAmount = request.BudgetAmount;
        project.StartDate = request.StartDate;
        project.EndDate = request.EndDate;
        project.DeadlineDate = request.DeadlineDate;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
