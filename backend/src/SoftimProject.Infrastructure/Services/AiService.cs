using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Infrastructure.Services;

public sealed class AiService : IAiService
{
    private readonly IChatClient? _chatClient;

    public AiService(IConfiguration configuration, IChatClient? chatClient = null)
    {
        _chatClient = chatClient;
    }

    public async Task<(string Summary, int TokensUsed)> SummarizeTicketAsync(string title, string description, IEnumerable<string> comments, CancellationToken cancellationToken = default)
    {
        if (_chatClient is null)
            return ("AI service not configured.", 0);

        var prompt = new StringBuilder();
        prompt.AppendLine("Summarize the following ticket concisely for a project manager. Include key points, current status, and any blockers.");
        prompt.AppendLine();
        prompt.AppendLine($"Title: {title}");
        prompt.AppendLine($"Description: {description}");
        prompt.AppendLine();
        prompt.AppendLine("Comments:");
        foreach (var comment in comments)
        {
            prompt.AppendLine($"- {comment}");
        }

        var response = await _chatClient.GetResponseAsync(prompt.ToString(), cancellationToken: cancellationToken);
        var tokensUsed = (int)(response.Usage?.TotalTokenCount ?? 0);

        return (response.Text, tokensUsed);
    }

    public async Task<(string Report, int TokensUsed)> GenerateReportAsync(string projectName, string reportType, string periodDescription, string data, CancellationToken cancellationToken = default)
    {
        if (_chatClient is null)
            return ("AI service not configured.", 0);

        var prompt = $"""
            Generate a {reportType} report for project "{projectName}" covering {periodDescription}.
            Use Markdown formatting. Include sections for: Summary, Key Achievements, Risks/Issues, and Recommendations.

            Data:
            {data}
            """;

        var response = await _chatClient.GetResponseAsync(prompt, cancellationToken: cancellationToken);
        var tokensUsed = (int)(response.Usage?.TotalTokenCount ?? 0);

        return (response.Text, tokensUsed);
    }

    public async Task<string?> SuggestStatusTransitionAsync(string ticketTitle, string currentStatus, string latestComment, CancellationToken cancellationToken = default)
    {
        if (_chatClient is null)
            return null;

        var prompt = $"""
            Based on the following ticket information, suggest if the status should be changed.
            Only suggest a change if it's clearly implied. Valid statuses: Backlog, Todo, InProgress, Review, Done, Closed.

            Ticket: {ticketTitle}
            Current Status: {currentStatus}
            Latest Comment: {latestComment}

            Respond with ONLY the new status name, or "NO_CHANGE" if no change is needed.
            """;

        var response = await _chatClient.GetResponseAsync(prompt, cancellationToken: cancellationToken);
        var suggestion = response.Text.Trim();

        return suggestion == "NO_CHANGE" ? null : suggestion;
    }
}
