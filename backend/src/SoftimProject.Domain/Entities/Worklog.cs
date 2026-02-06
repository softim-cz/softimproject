using SoftimProject.Domain.Common;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Domain.Entities;

public class Worklog : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Guid? TicketId { get; set; }
    public Guid UserId { get; set; }
    public DateOnly Date { get; set; }
    public decimal Hours { get; set; }
    public string? Description { get; set; }
    public WorklogSource Source { get; set; }
    public bool IsBillable { get; set; }
    public decimal? HourlyRateSnapshot { get; set; }

    // Navigation properties
    public Project Project { get; set; } = null!;
    public Ticket? Ticket { get; set; }
    public User User { get; set; } = null!;
}
