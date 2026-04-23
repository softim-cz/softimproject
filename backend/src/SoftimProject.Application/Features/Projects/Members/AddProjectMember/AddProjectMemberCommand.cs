using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Projects.Members.AddProjectMember;

public sealed record AddProjectMemberCommand(
    Guid ProjectId,
    Guid UserId,
    ProjectRole Role,
    decimal? HourlyRateOverride) : IRequest<Guid>, IRequireProjectRole
{
    public ProjectRole RequiredProjectRole => ProjectRole.ProjectManager;
}

public sealed class AddProjectMemberCommandValidator : AbstractValidator<AddProjectMemberCommand>
{
    public AddProjectMemberCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Role).IsInEnum();
        RuleFor(x => x.HourlyRateOverride).GreaterThan(0).When(x => x.HourlyRateOverride.HasValue);
    }
}

public sealed class AddProjectMemberCommandHandler(
    IApplicationDbContext dbContext) : IRequestHandler<AddProjectMemberCommand, Guid>
{
    public async Task<Guid> Handle(AddProjectMemberCommand request, CancellationToken cancellationToken)
    {
        var alreadyMember = await dbContext.ProjectMembers
            .AnyAsync(pm => pm.ProjectId == request.ProjectId && pm.UserId == request.UserId, cancellationToken);

        if (alreadyMember)
            throw new InvalidOperationException("User is already a member of this project.");

        var member = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            UserId = request.UserId,
            Role = request.Role,
            HourlyRateOverride = request.HourlyRateOverride,
            JoinedAt = DateTime.UtcNow
        };

        dbContext.ProjectMembers.Add(member);
        await dbContext.SaveChangesAsync(cancellationToken);

        return member.Id;
    }
}
