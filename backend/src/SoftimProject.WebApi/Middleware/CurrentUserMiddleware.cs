using SoftimProject.Application.Interfaces;

namespace SoftimProject.WebApi.Middleware;

public sealed class CurrentUserMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ICurrentUserService currentUserService)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            await currentUserService.InitializeAsync(context.RequestAborted);
        }

        await next(context);
    }
}
