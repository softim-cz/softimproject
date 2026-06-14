using System.ClientModel;
using System.Text;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Infrastructure.Services;

public sealed class AiService : IAiService
{
    // Null when no Azure OpenAI connection is configured (AzureOpenAI:Endpoint /
    // ApiKey / DeploymentName). In that case every call returns an empty result
    // and callers surface "AI not configured" instead of generating.
    private readonly ChatClient? _chat;

    public AiService(IConfiguration configuration)
    {
        var endpoint = configuration["AzureOpenAI:Endpoint"];
        var apiKey = configuration["AzureOpenAI:ApiKey"];
        var deployment = configuration["AzureOpenAI:DeploymentName"];

        if (!string.IsNullOrWhiteSpace(endpoint)
            && !string.IsNullOrWhiteSpace(apiKey)
            && !string.IsNullOrWhiteSpace(deployment))
        {
            var client = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apiKey));
            _chat = client.GetChatClient(deployment);
        }
    }

    public async Task<(string Summary, AiTokenUsage Usage, string Prompt)> SummarizeTicketAsync(string title, string description, IEnumerable<string> comments, CancellationToken cancellationToken = default)
    {
        var prompt = BuildSummarizePrompt(title, description, comments);
        if (_chat is null)
            return (string.Empty, new AiTokenUsage(0, 0), prompt);

        var (text, usage) = await CompleteAsync(prompt, cancellationToken);
        return (text, usage, prompt);
    }

    public async Task<(string Report, AiTokenUsage Usage, string Prompt)> GenerateReportAsync(string projectName, string reportType, string periodDescription, string data, CancellationToken cancellationToken = default)
    {
        var prompt = $"""
            Generate a {reportType} report for project "{projectName}" covering {periodDescription}.
            Use Markdown formatting. Include sections for: Summary, Key Achievements, Risks/Issues, and Recommendations.

            Data:
            {data}
            """;
        if (_chat is null)
            return (string.Empty, new AiTokenUsage(0, 0), prompt);

        var (text, usage) = await CompleteAsync(prompt, cancellationToken);
        return (text, usage, prompt);
    }

    public async Task<string?> SuggestStatusTransitionAsync(string ticketTitle, string currentStatus, string latestComment, CancellationToken cancellationToken = default)
    {
        if (_chat is null)
            return null;

        var prompt = $"""
            Based on the following ticket information, suggest if the status should be changed.
            Only suggest a change if it's clearly implied. Valid statuses: Backlog, Todo, InProgress, Review, Done, Closed.

            Ticket: {ticketTitle}
            Current Status: {currentStatus}
            Latest Comment: {latestComment}

            Respond with ONLY the new status name, or "NO_CHANGE" if no change is needed.
            """;

        var (text, _) = await CompleteAsync(prompt, cancellationToken);
        var suggestion = text.Trim();
        return suggestion == "NO_CHANGE" || string.IsNullOrEmpty(suggestion) ? null : suggestion;
    }

    private async Task<(string Text, AiTokenUsage Usage)> CompleteAsync(string prompt, CancellationToken cancellationToken)
    {
        ChatCompletion completion = await _chat!.CompleteChatAsync(
            [new UserChatMessage(prompt)], cancellationToken: cancellationToken);

        var text = completion.Content.Count > 0 ? completion.Content[0].Text.Trim() : string.Empty;
        var usage = completion.Usage is { } u
            ? new AiTokenUsage(u.InputTokenCount, u.OutputTokenCount)
            : new AiTokenUsage(0, 0);
        return (text, usage);
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
}
