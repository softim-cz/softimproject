using SoftimProject.Domain.Common;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Domain.Entities;

public class User : BaseEntity
{
    public string EntraObjectId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public GlobalRole GlobalRole { get; set; }
    public bool IsActive { get; set; }

    // Navigation properties
    public ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();
    public ICollection<Worklog> Worklogs { get; set; } = new List<Worklog>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
