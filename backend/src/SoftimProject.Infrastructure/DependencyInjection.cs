using System.Net;
using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using SoftimProject.Application.Interfaces;
using SoftimProject.Infrastructure.BackgroundServices;
using SoftimProject.Infrastructure.Persistence;
using SoftimProject.Infrastructure.Services;
using SoftimProject.Infrastructure.Services.EasyProject;

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
        services.AddHostedService<GitHubSyncService>();

        // EasyProject Migration
        services.AddHttpClient<IEasyProjectApiClient, EasyProjectApiClient>()
            .AddResilienceHandler("EasyProject", builder =>
            {
                builder.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 5,
                    BackoffType = DelayBackoffType.Exponential,
                    Delay = TimeSpan.FromSeconds(2),
                    UseJitter = true,
                    ShouldHandle = args => ValueTask.FromResult(
                        args.Outcome.Result?.StatusCode == HttpStatusCode.TooManyRequests),
                    DelayGenerator = args =>
                    {
                        if (args.Outcome.Result?.Headers.RetryAfter?.Delta is { } retryAfter)
                            return ValueTask.FromResult<TimeSpan?>(retryAfter);
                        return ValueTask.FromResult<TimeSpan?>(null); // fall back to exponential
                    }
                });

                builder.AddRateLimiter(new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 8,
                    Window = TimeSpan.FromSeconds(10),
                    SegmentsPerWindow = 2,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 50
                }));
            });
        services.AddSingleton<IMigrationProgressTracker, MigrationProgressTracker>();
        services.AddTransient<IEasyProjectMigrationService, EasyProjectMigrationService>();

        return services;
    }
}
