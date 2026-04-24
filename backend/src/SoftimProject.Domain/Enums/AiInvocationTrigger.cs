namespace SoftimProject.Domain.Enums;

public enum AiInvocationTrigger
{
    // Autonomous — AiSummarizationService, WeeklyReportService ticking on their own.
    AutoSummarize,
    WeeklyReport,
    // Human-initiated via API / UI; comes with an explicit Reason for the audit row.
    ManualResummarize,
    // DLQ replay of a previously failed auto-summarize.
    Replay
}
