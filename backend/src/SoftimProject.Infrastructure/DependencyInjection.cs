using System.Net;
using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.Retry;
using SoftimProject.Application.Interfaces;
using SoftimProject.Infrastructure.BackgroundServices;
using SoftimProject.Infrastructure.Persistence;
using SoftimProject.Infrastructure.Services;
using SoftimProject.Infrastructure.Services.EasyProject;
using SoftimProject.Infrastructure.Services.Email;

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

        // GitHub App (server-to-server). Opt-in: no-ops when GitHubApp config is empty.
        services.AddOptions<Options.GitHubAppOptions>().Bind(configuration.GetSection(Options.GitHubAppOptions.SectionName));
        services.AddSingleton<IGitHubAppTokenService, GitHubAppTokenService>();
        services.AddScoped<INotificationService, NotificationService>();

        // Background job observability (#12). Singleton registry so TrackedBackgroundService
        // ctors register themselves once at startup; recorder is singleton too — it only
        // holds factories and creates its own scopes to write JobRun rows.
        services.AddSingleton<IJobRegistry, JobRegistry>();
        services.AddSingleton<IJobRunRecorder, JobRunRecorder>();

        // Retry + dead-letter (#13). Two named Polly pipelines for non-HttpClient call sites
        // (Octokit via AiSummarization / GitHubSync, and Microsoft.Extensions.AI IChatClient).
        // HTTP-client integrations keep their own AddResilienceHandler pipeline (see Easy
        // Project below) — these are for direct SDK calls where we don't own the HttpClient.
        services.AddResiliencePipeline(ResiliencePipelines.AiApi, builder =>
        {
            builder.AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(2),
                UseJitter = true,
            });
        });
        services.AddResiliencePipeline(ResiliencePipelines.GitHubApi, builder =>
        {
            builder.AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 4,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(2),
                UseJitter = true,
            });
        });
        // Stateless — they all use IServiceScopeFactory to open their own DbContext scopes.
        services.AddSingleton<IDeadLetterQueue, DeadLetterQueue>();
        services.AddSingleton<IDeadLetterReplayer, DeadLetterReplayer>();
        services.AddSingleton<IDeadLetterReplayHandler, AiSummarizeTicketReplayHandler>();

        // AI audit + rate-limit (#16). Stateless — opens its own DbContext scopes.
        services.AddSingleton<IAiInvocationRecorder, AiInvocationRecorder>();

        // Background Services
        services.AddHostedService<EmailPollingService>();

        // Email-to-ticket sync (#49). Gated by Sync:Email:Enabled — when false, the hosted
        // service short-circuits each tick without touching Graph. GraphMailboxClient is
        // resolved lazily on the first iteration, so misconfigured secrets don't crash
        // startup; they surface as JobRun failures with a clear error.
        services.AddOptions<EmailSyncOptions>().Bind(configuration.GetSection("Sync:Email"));
        services.AddScoped<IEmailMailboxClient, GraphMailboxClient>();
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
