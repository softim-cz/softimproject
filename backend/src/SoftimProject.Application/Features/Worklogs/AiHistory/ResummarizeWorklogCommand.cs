using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Worklogs.AiHistory;

public sealed record ResummarizeWorklogCommand(Guid ProjectId, Guid WorklogId)
    : IRequest<Guid>, IRequireProjectRole
{
    // Developer+ can trigger a manual AI run — AI cost is real, Guest is excluded.
    public ProjectRole RequiredProjectRole => ProjectRole.Developer;
}

public sealed class ResummarizeWorklogCommandValidator : AbstractValidator<ResummarizeWorklogCommand>
{
    public ResummarizeWorklogCommandValidator()
    {
        RuleFor(x => x.WorklogId).NotEmpty();
    }
}

public sealed class ResummarizeWorklogCommandHandler(
    IApplicationDbContext dbContext,
    IAiService aiService,
    IAiInvocationRecorder recorder,
    ICurrentUserService currentUser) : IRequestHandler<ResummarizeWorklogCommand, Guid>
{
    public async Task<Guid> Handle(ResummarizeWorklogCommand request, CancellationToken cancellationToken)
    {
        var worklog = await dbContext.GetWorklogForProjectAsync(request.ProjectId, request.WorklogId, cancellationToken);

        var ticketTitle = await dbContext.Tickets
            .Where(t => t.Id == worklog.TicketId)
            .Select(t => t.Title)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        var recorded = await recorder.RecordAsync(
            new AiInvocationContext(
                AiInvocationTrigger.ManualResummarize,
                InputText: $"worklog:{worklog.Id}|ticket:{worklog.TicketId}|desc:{worklog.Description}",
                TriggeredByUserId: currentUser.UserId,
                ProjectId: request.ProjectId,
                TicketId: worklog.TicketId,
                Reason: "Worklog AI summary"),
            async ct =>
            {
                var (summary, usage, _) = await aiService.SummarizeWorklogAsync(
                    ticketTitle, worklog.Description, ct);
                return new AiInvocationCall<string>(summary, usage.PromptTokens, usage.CompletionTokens, summary);
            },
            cancellationToken);

        if (string.IsNullOrWhiteSpace(recorded.Payload))
        {
            // AI returned nothing — almost always because no chat client is configured
            // (no Azure OpenAI endpoint/key). Surface it instead of silently storing empty.
            throw new ValidationException(
                "AI nevrátila žádný výstup. Generování AI souhrnů není nakonfigurované (chybí připojení k Azure OpenAI).");
        }

        worklog.AiSummary = recorded.Payload;
        await dbContext.SaveChangesAsync(cancellationToken);

        return recorded.InvocationId;
    }
}
