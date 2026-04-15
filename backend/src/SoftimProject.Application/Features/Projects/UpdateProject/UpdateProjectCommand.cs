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
    string? Code,
    string? Description,
    ProjectStatus Status,
    decimal? BudgetHours,
    decimal? BudgetAmount,
    DateOnly? StartDate,
    DateOnly? EndDate,
    DateOnly? DeadlineDate,
    Guid? CompanyId = null,
    Guid? ProjectTypeId = null,
    Guid? ProjectStateId = null,
    Guid? ParentProjectId = null,
    string? ExternalSystem = null,
    string? ExternalProjectId = null,
    string? ExternalApiToken = null,
    string? WebhookSecret = null,
    bool? ClientAccessEnabled = null) : IRequest, IRequireProjectAccess
{
    public Guid ProjectId => Id;
}

public sealed class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).MinimumLength(2).MaximumLength(6)
            .Matches("^[A-Z]+$").WithMessage("Code must be uppercase letters only.")
            .When(x => !string.IsNullOrEmpty(x.Code));
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
        project.CompanyId = request.CompanyId;
        project.ProjectTypeId = request.ProjectTypeId;
        project.ProjectStateId = request.ProjectStateId;
        project.ParentProjectId = request.ParentProjectId;
        project.ExternalSystem = request.ExternalSystem;
        project.ExternalProjectId = request.ExternalProjectId;
        project.ExternalApiToken = request.ExternalApiToken;
        project.WebhookSecret = request.WebhookSecret;

        if (!string.IsNullOrEmpty(request.Code) && request.Code != project.Code)
        {
            var codeExists = await dbContext.Projects
                .AnyAsync(p => p.Code == request.Code && p.Id != request.Id, cancellationToken);
            if (codeExists)
                throw new ValidationException("Code is already in use by another project.");

            project.Code = request.Code;
        }

        if (request.ClientAccessEnabled.HasValue)
        {
            project.ClientAccessEnabled = request.ClientAccessEnabled.Value;

            if (request.ClientAccessEnabled.Value && string.IsNullOrEmpty(project.ClientAccessToken))
            {
                project.ClientAccessToken = Guid.NewGuid().ToString("N");
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
