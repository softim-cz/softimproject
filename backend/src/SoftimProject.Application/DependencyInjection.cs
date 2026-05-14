using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SoftimProject.Application.Behaviors;

namespace SoftimProject.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            // Authorization runs before validation: an unauthenticated/unauthorized
            // caller must be rejected with 401/403 without leaking validation
            // feedback about the payload they were not allowed to submit.
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
