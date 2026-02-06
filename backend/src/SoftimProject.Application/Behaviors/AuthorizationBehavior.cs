using MediatR;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Behaviors;

public sealed class AuthorizationBehavior<TRequest, TResponse>(ICurrentUserService currentUserService)
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

        if (request is IRequireRole roleRequest)
        {
            var hasRole = currentUserService.IsInRole(roleRequest.RequiredRole);
            if (!hasRole)
                throw new UnauthorizedAccessException($"User does not have the required role: {roleRequest.RequiredRole}");
        }

        return await next(cancellationToken);
    }
}
