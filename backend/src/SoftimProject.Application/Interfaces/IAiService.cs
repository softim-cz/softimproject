namespace SoftimProject.Application.Interfaces;

public sealed record AiTokenUsage(int PromptTokens, int CompletionTokens)
{
    public int TotalTokens => PromptTokens + CompletionTokens;
}

// Return tuples carry the rendered prompt alongside the response so callers that
// record AiInvocation audit rows can hash the exact input without rebuilding it.
public interface IAiService
{
    Task<(string Summary, AiTokenUsage Usage, string Prompt)> SummarizeTicketAsync(
        string title,
        string description,
        IEnumerable<string> comments,
        CancellationToken cancellationToken = default);

    Task<(string Report, AiTokenUsage Usage, string Prompt)> GenerateReportAsync(
        string projectName,
        string reportType,
        string periodDescription,
        string data,
        CancellationToken cancellationToken = default);

    Task<string?> SuggestStatusTransitionAsync(
        string ticketTitle,
        string currentStatus,
        string latestComment,
        CancellationToken cancellationToken = default);
}
