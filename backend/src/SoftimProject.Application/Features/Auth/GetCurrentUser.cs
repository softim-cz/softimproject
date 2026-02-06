using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Auth;

public sealed record CurrentUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    GlobalRole GlobalRole,
    List<ProjectRoleDto> ProjectRoles);

public sealed record ProjectRoleDto(
    Guid ProjectId,
    string ProjectName,
    ProjectRole Role);

public sealed record GetCurrentUserQuery : IRequest<CurrentUserDto>;

public sealed class GetCurrentUserQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService) : IRequestHandler<GetCurrentUserQuery, CurrentUserDto>
{
    public async Task<CurrentUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var entraObjectId = currentUserService.EntraObjectId;
        if (string.IsNullOrEmpty(entraObjectId))
            throw new UnauthorizedAccessException("User is not authenticated.");

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.EntraObjectId == entraObjectId, cancellationToken);

        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                EntraObjectId = entraObjectId,
                Email = currentUserService.Email ?? string.Empty,
                DisplayName = currentUserService.Email ?? "Unknown",
                GlobalRole = GlobalRole.User,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var projectRoles = await dbContext.ProjectMembers
            .Where(pm => pm.UserId == user.Id)
            .Select(pm => new ProjectRoleDto(pm.ProjectId, pm.Project.Name, pm.Role))
            .ToListAsync(cancellationToken);

        return new CurrentUserDto(
            user.Id,
            user.Email,
            user.DisplayName,
            user.AvatarUrl,
            user.GlobalRole,
            projectRoles);
    }
}
