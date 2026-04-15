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

    // Entra attributes
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? CorporateRole { get; set; }
    public string? CompanyName { get; set; }

    // GitHub OAuth
    public string? GitHubAccessToken { get; set; }
    public string? GitHubLogin { get; set; }

    // Navigation properties
    public ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();
    public ICollection<Worklog> Worklogs { get; set; } = new List<Worklog>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<UserApplicationRole> UserApplicationRoles { get; set; } = new List<UserApplicationRole>();
    public ICollection<ViewConfiguration> ViewConfigurations { get; set; } = new List<ViewConfiguration>();
}
