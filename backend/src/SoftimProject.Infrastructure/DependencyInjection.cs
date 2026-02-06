using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SoftimProject.Application.Interfaces;
using SoftimProject.Infrastructure.BackgroundServices;
using SoftimProject.Infrastructure.Persistence;
using SoftimProject.Infrastructure.Services;

namespace SoftimProject.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // EF Core
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        // Dapper
        services.AddSingleton<IDapperContext>(provider =>
            new DapperContext(configuration.GetConnectionString("DefaultConnection")!));

        // Services
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IBlobStorageService, BlobStorageService>();
        services.AddScoped<IAiService, AiService>();
        services.AddScoped<INotificationService, NotificationService>();

        // Background Services
        services.AddHostedService<JiraSyncService>();
        services.AddHostedService<RedmineSyncService>();
        services.AddHostedService<EmailPollingService>();
        services.AddHostedService<AiSummarizationService>();
        services.AddHostedService<DeadlineNotificationService>();
        services.AddHostedService<WeeklyReportService>();
        services.AddHostedService<HealthRecalcService>();

        return services;
    }
}
