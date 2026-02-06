using SoftimProject.Domain.Enums;

namespace SoftimProject.Domain.Entities;

public class ProjectMember
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    public ProjectRole Role { get; set; }
    public decimal? HourlyRateOverride { get; set; }
    public DateTime JoinedAt { get; set; }

    // Navigation properties
    public Project Project { get; set; } = null!;
    public User User { get; set; } = null!;
}
