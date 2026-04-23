using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Behaviors;

public sealed class AuthorizationBehavior<TRequest, TResponse>(
    ICurrentUserService currentUserService,
    IApplicationDbContext dbContext)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is IRequireProjectAccess projectRequest)
        {
            var hasAccess = await currentUserService.HasProjectAccessAsync(projectRequest.ProjectId, cancellationToken);
            if (!hasAccess)
                throw new UnauthorizedAccessException($"User does not have access to project {projectRequest.ProjectId}");
        }

        if (request is IRequireProjectRole projectRoleRequest)
        {
            // Admin bypasses the project-role check; project membership was already verified above.
            if (!currentUserService.IsInRole("Admin") && currentUserService.UserId.HasValue)
            {
                var userId = currentUserService.UserId.Value;
                var membershipRole = await dbContext.ProjectMembers
                    .Where(pm => pm.ProjectId == projectRoleRequest.ProjectId && pm.UserId == userId)
                    .Select(pm => (ProjectRole?)pm.Role)
                    .FirstOrDefaultAsync(cancellationToken);

                if (membershipRole is null || !SatisfiesProjectRole(membershipRole.Value, projectRoleRequest.RequiredProjectRole))
                    throw new UnauthorizedAccessException(
                        $"User does not have the required project role '{projectRoleRequest.RequiredProjectRole}' in project {projectRoleRequest.ProjectId}");
            }
        }

        if (request is IRequireRole roleRequest)
        {
            var hasRole = currentUserService.IsInRole(roleRequest.RequiredRole);
            if (!hasRole)
                throw new UnauthorizedAccessException($"User does not have the required role: {roleRequest.RequiredRole}");
        }

        if (request is IRequirePermission permRequest && currentUserService.UserId.HasValue)
        {
            // Admin bypasses permission checks
            if (!currentUserService.IsInRole("Admin"))
            {
                var userId = currentUserService.UserId.Value;
                var roles = await dbContext.UserApplicationRoles
                    .Where(uar => uar.UserId == userId)
                    .Select(uar => uar.ApplicationRole)
                    .ToListAsync(cancellationToken);

                var hasPermission = roles.Any(r => CheckPermission(r, permRequest.Area, permRequest.Operation));

                if (!hasPermission)
                    throw new UnauthorizedAccessException($"User does not have {permRequest.Operation} permission for {permRequest.Area}");
            }
        }

        return await next(cancellationToken);
    }

    // Hierarchy: ProjectManager ≫ Developer ≫ Guest. Admin is handled by bypass above.
    // A membership role satisfies a required role iff it is at least as privileged.
    private static bool SatisfiesProjectRole(ProjectRole membership, ProjectRole required) => required switch
    {
        ProjectRole.ProjectManager => membership == ProjectRole.ProjectManager,
        ProjectRole.Developer => membership == ProjectRole.ProjectManager || membership == ProjectRole.Developer,
        ProjectRole.Guest => true,
        _ => false
    };

    private static bool CheckPermission(ApplicationRole role, PermissionArea area, PermissionOperation operation)
    {
        return (area, operation) switch
        {
            (PermissionArea.Projects, PermissionOperation.Create) => role.ProjectsCreate,
            (PermissionArea.Projects, PermissionOperation.Read) => role.ProjectsRead,
            (PermissionArea.Projects, PermissionOperation.Update) => role.ProjectsUpdate,
            (PermissionArea.Projects, PermissionOperation.Delete) => role.ProjectsDelete,
            (PermissionArea.TimeTracking, PermissionOperation.Create) => role.TimeTrackingCreate,
            (PermissionArea.TimeTracking, PermissionOperation.Read) => role.TimeTrackingRead,
            (PermissionArea.TimeTracking, PermissionOperation.Update) => role.TimeTrackingUpdate,
            (PermissionArea.TimeTracking, PermissionOperation.Delete) => role.TimeTrackingDelete,
            (PermissionArea.Reports, PermissionOperation.Create) => role.ReportsCreate,
            (PermissionArea.Reports, PermissionOperation.Read) => role.ReportsRead,
            (PermissionArea.Reports, PermissionOperation.Update) => role.ReportsUpdate,
            (PermissionArea.Reports, PermissionOperation.Delete) => role.ReportsDelete,
            _ => false
        };
    }
}
