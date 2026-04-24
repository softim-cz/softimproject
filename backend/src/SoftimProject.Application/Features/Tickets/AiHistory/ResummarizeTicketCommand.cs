using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Tickets.AiHistory;

public sealed record ResummarizeTicketCommand(Guid ProjectId, Guid TicketId, string Reason)
    : IRequest<Guid>, IRequireProjectRole
{
    // Developer+ can trigger manual re-run. Guest explicitly not — AI cost is real.
    public ProjectRole RequiredProjectRole => ProjectRole.Developer;
}

public sealed class ResummarizeTicketCommandValidator : AbstractValidator<ResummarizeTicketCommand>
{
    public ResummarizeTicketCommandValidator()
    {
        // Manual re-run must justify itself — the audit row wants to answer "why" later.
        RuleFor(x => x.Reason).NotEmpty().MinimumLength(3).MaximumLength(500);
    }
}

public sealed class ResummarizeTicketCommandHandler(
    IApplicationDbContext dbContext,
    IAiService aiService,
    IAiInvocationRecorder recorder,
    ICurrentUserService currentUser) : IRequestHandler<ResummarizeTicketCommand, Guid>
{
    public async Task<Guid> Handle(ResummarizeTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await dbContext.Tickets
            .Include(t => t.Comments)
            .FirstOrDefaultAsync(t => t.Id == request.TicketId && t.ProjectId == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Ticket), request.TicketId);

        var comments = ticket.Comments.OrderBy(c => c.CreatedAt).Select(c => c.Content).ToList();

        var recorded = await recorder.RecordAsync(
            new AiInvocationContext(
                AiInvocationTrigger.ManualResummarize,
                InputText: $"ticket:{ticket.Id}|title:{ticket.Title}|desc:{ticket.Description}|comments:{comments.Count}",
                TriggeredByUserId: currentUser.UserId,
                ProjectId: ticket.ProjectId,
                TicketId: ticket.Id,
                Reason: request.Reason),
            async ct =>
            {
                var (summary, usage, _) = await aiService.SummarizeTicketAsync(
                    ticket.Title, ticket.Description ?? string.Empty, comments, ct);
                return new AiInvocationCall<string>(summary, usage.PromptTokens, usage.CompletionTokens, summary);
            },
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(recorded.Payload))
        {
            ticket.AiSummary = recorded.Payload;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return recorded.InvocationId;
    }
}
