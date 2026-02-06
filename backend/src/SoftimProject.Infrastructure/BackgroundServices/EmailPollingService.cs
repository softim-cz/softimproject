using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Infrastructure.BackgroundServices;

public sealed class EmailPollingService(IServiceScopeFactory scopeFactory, ILogger<EmailPollingService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(2));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

                // TODO: Implement email polling
                // 1. Connect to configured mailbox (IMAP/MS Graph)
                // 2. Read new emails matching project patterns
                // 3. Create tickets or comments from emails
                // 4. Mark emails as processed

                logger.LogDebug("Email polling cycle completed");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Email polling failed");
            }
        }
    }
}
