using SoftimProject.Domain.Enums;

namespace SoftimProject.Domain.Entities;

public class AiReport
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public AiReportType ReportType { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public string Content { get; set; } = string.Empty; // Markdown
    public int TokensUsed { get; set; }
    public DateTime GeneratedAt { get; set; }

    // Navigation properties
    public Project Project { get; set; } = null!;
}
