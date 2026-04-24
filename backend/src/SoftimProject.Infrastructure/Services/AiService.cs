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

    public async Task<(string Summary, AiTokenUsage Usage, string Prompt)> SummarizeTicketAsync(string title, string description, IEnumerable<string> comments, CancellationToken cancellationToken = default)
    {
        var prompt = BuildSummarizePrompt(title, description, comments);
        if (_chatClient is null)
            return (string.Empty, new AiTokenUsage(0, 0), prompt);

        var response = await _chatClient.GetResponseAsync(prompt, cancellationToken: cancellationToken);
        return (response.Text.Trim(), ExtractUsage(response), prompt);
    }

    public async Task<(string Report, AiTokenUsage Usage, string Prompt)> GenerateReportAsync(string projectName, string reportType, string periodDescription, string data, CancellationToken cancellationToken = default)
    {
        var prompt = $"""
            Generate a {reportType} report for project "{projectName}" covering {periodDescription}.
            Use Markdown formatting. Include sections for: Summary, Key Achievements, Risks/Issues, and Recommendations.

            Data:
            {data}
            """;
        if (_chatClient is null)
            return (string.Empty, new AiTokenUsage(0, 0), prompt);

        var response = await _chatClient.GetResponseAsync(prompt, cancellationToken: cancellationToken);
        return (response.Text.Trim(), ExtractUsage(response), prompt);
    }

    // UsageDetails exposes Input/Output separately on Microsoft.Extensions.AI 10.x.
    // Older providers may populate only total — in that case we assign it to output
    // (the more expensive slot — conservative over-estimate for cost audit).
    private static AiTokenUsage ExtractUsage(ChatResponse response)
    {
        var usage = response.Usage;
        if (usage is null) return new AiTokenUsage(0, 0);
        var input = (int)(usage.InputTokenCount ?? 0);
        var output = (int)(usage.OutputTokenCount ?? 0);
        if (input == 0 && output == 0 && usage.TotalTokenCount is long total)
            output = (int)total;
        return new AiTokenUsage(input, output);
    }

    internal static string BuildSummarizePrompt(string title, string description, IEnumerable<string> comments)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine("Summarize the following ticket concisely for a project manager. Include key points, current status, and any blockers.");
        prompt.AppendLine();
        prompt.AppendLine($"Title: {title}");
        prompt.AppendLine($"Description: {description}");
        prompt.AppendLine();
        prompt.AppendLine("Comments:");
        foreach (var comment in comments)
            prompt.AppendLine($"- {comment}");
        return prompt.ToString();
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
