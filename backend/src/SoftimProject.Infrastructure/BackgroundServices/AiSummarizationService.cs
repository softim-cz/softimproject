using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Infrastructure.BackgroundServices;

public sealed class AiSummarizationService(
    IServiceScopeFactory scopeFactory,
    IJobRegistry jobRegistry,
    ILogger<AiSummarizationService> logger)
    : TrackedBackgroundService(scopeFactory, jobRegistry, logger, TimeSpan.FromHours(6))
{
    protected override async Task ExecuteIterationAsync(
        IServiceProvider services,
        IJobRunScope run,
        CancellationToken cancellationToken)
    {
        var dbContext = services.GetRequiredService<IApplicationDbContext>();
        var aiService = services.GetRequiredService<IAiService>();

        var ticketsToSummarize = await dbContext.Tickets
            .Include(t => t.Comments)
            .Include(t => t.TaskState)
            .Where(t => !t.TaskState.IsClosedState
                && t.Comments.Count >= 3
                && (t.AiSummary == null || t.Comments.Any(c => c.CreatedAt > t.UpdatedAt)))
            .Take(50)
            .ToListAsync(cancellationToken);

        var summarizedCount = 0;
        var failed = 0;
        foreach (var ticket in ticketsToSummarize)
        {
            try
            {
                var comments = ticket.Comments.OrderBy(c => c.CreatedAt).Select(c => c.Content);
                var (summary, _) = await aiService.SummarizeTicketAsync(
                    ticket.Title,
                    ticket.Description ?? string.Empty,
                    comments,
                    cancellationToken);

                if (string.IsNullOrWhiteSpace(summary))
                    continue;

                ticket.AiSummary = summary;
                summarizedCount++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to summarize ticket {TicketId}", ticket.Id);
                failed++;
            }
        }

        if (summarizedCount > 0)
            await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("AI summarization completed for {Count} tickets", summarizedCount);
        run.MarkSuccess(summarizedCount, failed);
    }
}
