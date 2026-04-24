using SoftimProject.Domain.Enums;

namespace SoftimProject.Domain.Entities;

// Audit record for every call into the AI provider (Azure OpenAI or any future
// IChatClient). One row per invocation with what, when, why, how much.
public class AiInvocation
{
    public Guid Id { get; set; }

    public AiInvocationTrigger Trigger { get; set; }
    public Guid? TriggeredByUserId { get; set; }
    public User? TriggeredByUser { get; set; }

    public Guid? ProjectId { get; set; }
    public Project? Project { get; set; }
    public Guid? TicketId { get; set; }
    public Ticket? Ticket { get; set; }

    // Stable hash of the prompt input. Lets us deduplicate / spot repeated calls on
    // the same content without storing the prompt verbatim (which can be large).
    public string InputHash { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;

    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
    public decimal EstimatedCostUsd { get; set; }

    // First N chars of the output. Full payload tends to be large and we can always
    // look at the resulting Ticket.AiSummary / AiReport for the canonical version.
    public string? OutputPreview { get; set; }

    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long? DurationMs { get; set; }

    // Required for ManualResummarize; optional/null for autonomous triggers.
    public string? Reason { get; set; }
}
