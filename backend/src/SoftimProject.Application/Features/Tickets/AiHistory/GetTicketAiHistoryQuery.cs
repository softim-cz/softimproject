using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Tickets.AiHistory;

public sealed record AiInvocationDto(
    Guid Id,
    AiInvocationTrigger Trigger,
    string? TriggeredByDisplayName,
    string Model,
    int PromptTokens,
    int CompletionTokens,
    int TotalTokens,
    decimal EstimatedCostUsd,
    string? OutputPreview,
    bool Success,
    string? ErrorMessage,
    DateTime StartedAt,
    long? DurationMs,
    string? Reason);

public sealed record GetTicketAiHistoryQuery(Guid ProjectId, Guid TicketId)
    : IRequest<List<AiInvocationDto>>, IRequireProjectAccess;

public sealed class GetTicketAiHistoryQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetTicketAiHistoryQuery, List<AiInvocationDto>>
{
    public async Task<List<AiInvocationDto>> Handle(GetTicketAiHistoryQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.AiInvocations
            .AsNoTracking()
            .Where(i => i.TicketId == request.TicketId && i.ProjectId == request.ProjectId)
            .OrderByDescending(i => i.StartedAt)
            .Select(i => new AiInvocationDto(
                i.Id,
                i.Trigger,
                i.TriggeredByUser != null ? i.TriggeredByUser.DisplayName : null,
                i.Model,
                i.PromptTokens,
                i.CompletionTokens,
                i.TotalTokens,
                i.EstimatedCostUsd,
                i.OutputPreview,
                i.Success,
                i.ErrorMessage,
                i.StartedAt,
                i.DurationMs,
                i.Reason))
            .ToListAsync(cancellationToken);
    }
}
