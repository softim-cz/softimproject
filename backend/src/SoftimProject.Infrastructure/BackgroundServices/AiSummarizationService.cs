using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly.Registry;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

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
        var pipeline = services.GetRequiredService<ResiliencePipelineProvider<string>>()
            .GetPipeline(ResiliencePipelines.AiApi);
        var deadLetters = services.GetRequiredService<IDeadLetterQueue>();
        var recorder = services.GetRequiredService<IAiInvocationRecorder>();

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
                var comments = ticket.Comments.OrderBy(c => c.CreatedAt).Select(c => c.Content).ToList();

                // Audit + rate-limit wrapper around the retry pipeline: one AiInvocation
                // row per logical call, retries are invisible to the audit log (they
                // succeed or all fail together).
                var ctx = new AiInvocationContext(
                    AiInvocationTrigger.AutoSummarize,
                    InputText: $"ticket:{ticket.Id}|title:{ticket.Title}|desc:{ticket.Description}|comments:{comments.Count}",
                    TriggeredByUserId: null, // background — no per-user rate limit here
                    ProjectId: ticket.ProjectId,
                    TicketId: ticket.Id);

                var recorded = await recorder.RecordAsync(
                    ctx,
                    async ct =>
                    {
                        var result = await pipeline.ExecuteAsync(
                            async ct2 => await aiService.SummarizeTicketAsync(
                                ticket.Title,
                                ticket.Description ?? string.Empty,
                                comments,
                                ct2),
                            ct);
                        return new AiInvocationCall<string>(
                            result.Summary,
                            result.Usage.PromptTokens,
                            result.Usage.CompletionTokens,
                            result.Summary);
                    },
                    cancellationToken);

                var summary = recorded.Payload;
                if (string.IsNullOrWhiteSpace(summary))
                    continue;

                ticket.AiSummary = summary;
                summarizedCount++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to summarize ticket {TicketId} after retries", ticket.Id);
                failed++;

                // Final failure — hand over to DLQ for admin-driven replay.
                var payload = JsonSerializer.Serialize(new
                {
                    ticketId = ticket.Id,
                    ticketTitle = ticket.Title,
                    commentCount = ticket.Comments.Count,
                });
                await deadLetters.EnqueueAsync(
                    DeadLetterOperation.AiSummarizeTicket,
                    ticket.Id.ToString(),
                    payload,
                    ex,
                    cancellationToken);
            }
        }

        if (summarizedCount > 0)
            await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("AI summarization completed for {Count} tickets", summarizedCount);
        run.MarkSuccess(summarizedCount, failed);
    }
}
