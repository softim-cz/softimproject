using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Infrastructure.BackgroundServices;

public sealed class EmailPollingService(
    IServiceScopeFactory scopeFactory,
    IJobRegistry jobRegistry,
    ILogger<EmailPollingService> logger)
    : TrackedBackgroundService(scopeFactory, jobRegistry, logger, TimeSpan.FromMinutes(2))
{
    protected override Task ExecuteIterationAsync(
        IServiceProvider services,
        IJobRunScope run,
        CancellationToken cancellationToken)
    {
        // TODO: Implement email polling
        // 1. Connect to configured mailbox (IMAP/MS Graph)
        // 2. Read new emails matching project patterns
        // 3. Create tickets or comments from emails
        // 4. Mark emails as processed
        logger.LogDebug("Email polling cycle completed");
        run.MarkSuccess(itemsProcessed: 0);
        return Task.CompletedTask;
    }
}
