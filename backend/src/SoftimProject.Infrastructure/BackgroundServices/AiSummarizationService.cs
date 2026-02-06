using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Infrastructure.BackgroundServices;

public sealed class AiSummarizationService(IServiceScopeFactory scopeFactory, ILogger<AiSummarizationService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(6));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                var aiService = scope.ServiceProvider.GetRequiredService<IAiService>();

                var ticketsToSummarize = await dbContext.Tickets
                    .Include(t => t.Comments)
                    .Where(t => t.Status != TicketStatus.Closed
                        && t.Comments.Count >= 3
                        && (t.AiSummary == null || t.Comments.Any(c => c.CreatedAt > t.UpdatedAt)))
                    .Take(50)
                    .ToListAsync(stoppingToken);

                foreach (var ticket in ticketsToSummarize)
                {
                    try
                    {
                        var comments = ticket.Comments.OrderBy(c => c.CreatedAt).Select(c => c.Content);
                        var (summary, _) = await aiService.SummarizeTicketAsync(
                            ticket.Title, ticket.Description ?? "", comments, stoppingToken);
                        ticket.AiSummary = summary;
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to summarize ticket {TicketId}", ticket.Id);
                    }
                }

                await dbContext.SaveChangesAsync(stoppingToken);
                logger.LogInformation("AI summarization completed for {Count} tickets", ticketsToSummarize.Count);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AI summarization service failed");
            }
        }
    }
}
