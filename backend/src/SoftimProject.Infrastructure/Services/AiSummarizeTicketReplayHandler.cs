using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Infrastructure.Services;

// Replay strategy for AiSummarizeTicket: OperationKey is the ticket id, payload is
// unused (ticket state is authoritative from DB). Idempotent — worst case we
// overwrite AiSummary with the same content.
public sealed class AiSummarizeTicketReplayHandler(IServiceScopeFactory scopeFactory)
    : IDeadLetterReplayHandler
{
    public DeadLetterOperation OperationType => DeadLetterOperation.AiSummarizeTicket;

    public async Task<ReplayOutcome> ReplayAsync(DeadLetterEntry entry, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(entry.OperationKey, out var ticketId))
            return new ReplayOutcome(false, $"Invalid OperationKey '{entry.OperationKey}' for AiSummarizeTicket — expected a ticket GUID.");

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var aiService = scope.ServiceProvider.GetRequiredService<IAiService>();

        var ticket = await db.Tickets
            .Include(t => t.Comments)
            .FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken);

        if (ticket is null)
            return new ReplayOutcome(false, $"Ticket {ticketId} no longer exists.");

        var comments = ticket.Comments.OrderBy(c => c.CreatedAt).Select(c => c.Content).ToList();
        var recorder = scope.ServiceProvider.GetRequiredService<IAiInvocationRecorder>();
        var recorded = await recorder.RecordAsync(
            new AiInvocationContext(
                AiInvocationTrigger.Replay,
                InputText: $"ticket:{ticket.Id}|title:{ticket.Title}|desc:{ticket.Description}|comments:{comments.Count}",
                TriggeredByUserId: null,
                ProjectId: ticket.ProjectId,
                TicketId: ticket.Id,
                Reason: "DLQ replay"),
            async ct =>
            {
                var (summary, usage, _) = await aiService.SummarizeTicketAsync(
                    ticket.Title, ticket.Description ?? string.Empty, comments, userInstruction: null, ct);
                return new AiInvocationCall<string>(summary, usage.PromptTokens, usage.CompletionTokens, summary);
            },
            cancellationToken);
        var replaySummary = recorded.Payload;

        if (string.IsNullOrWhiteSpace(replaySummary))
            return new ReplayOutcome(false, "AI service returned an empty summary.");

        ticket.AiSummary = replaySummary;
        await db.SaveChangesAsync(cancellationToken);
        return new ReplayOutcome(true);
    }
}
