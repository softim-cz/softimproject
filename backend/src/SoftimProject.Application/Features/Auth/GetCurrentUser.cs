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
    string? FirstName,
    string? LastName,
    string? CorporateRole,
    string? CompanyName,
    List<ProjectRoleDto> ProjectRoles,
    UserPermissionsDto Permissions);

public sealed record ProjectRoleDto(
    Guid ProjectId,
    string ProjectName,
    ProjectRole Role);

public sealed record UserPermissionsDto(
    bool ProjectsCreate,
    bool ProjectsRead,
    bool ProjectsUpdate,
    bool ProjectsDelete,
    bool TimeTrackingCreate,
    bool TimeTrackingRead,
    bool TimeTrackingUpdate,
    bool TimeTrackingDelete,
    bool ReportsCreate,
    bool ReportsRead,
    bool ReportsUpdate,
    bool ReportsDelete);

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

        // Aggregate permissions from all assigned application roles
        var isAdmin = user.GlobalRole == GlobalRole.Admin;
        UserPermissionsDto permissions;

        if (isAdmin)
        {
            // Admin gets all permissions
            permissions = new UserPermissionsDto(true, true, true, true, true, true, true, true, true, true, true, true);
        }
        else
        {
            var roles = await dbContext.UserApplicationRoles
                .Where(uar => uar.UserId == user.Id)
                .Select(uar => uar.ApplicationRole)
                .ToListAsync(cancellationToken);

            permissions = new UserPermissionsDto(
                roles.Any(r => r.ProjectsCreate),
                roles.Any(r => r.ProjectsRead),
                roles.Any(r => r.ProjectsUpdate),
                roles.Any(r => r.ProjectsDelete),
                roles.Any(r => r.TimeTrackingCreate),
                roles.Any(r => r.TimeTrackingRead),
                roles.Any(r => r.TimeTrackingUpdate),
                roles.Any(r => r.TimeTrackingDelete),
                roles.Any(r => r.ReportsCreate),
                roles.Any(r => r.ReportsRead),
                roles.Any(r => r.ReportsUpdate),
                roles.Any(r => r.ReportsDelete));
        }

        return new CurrentUserDto(
            user.Id,
            user.Email,
            user.DisplayName,
            user.AvatarUrl,
            user.GlobalRole,
            user.FirstName,
            user.LastName,
            user.CorporateRole,
            user.CompanyName,
            projectRoles,
            permissions);
    }
}
