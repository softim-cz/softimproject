using SoftimProject.Domain.Common;

namespace SoftimProject.Domain.Entities;

public class ApplicationRole : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }

    // CRUD permissions per area
    public bool ProjectsCreate { get; set; }
    public bool ProjectsRead { get; set; }
    public bool ProjectsUpdate { get; set; }
    public bool ProjectsDelete { get; set; }

    public bool TimeTrackingCreate { get; set; }
    public bool TimeTrackingRead { get; set; }
    public bool TimeTrackingUpdate { get; set; }
    public bool TimeTrackingDelete { get; set; }

    public bool ReportsCreate { get; set; }
    public bool ReportsRead { get; set; }
    public bool ReportsUpdate { get; set; }
    public bool ReportsDelete { get; set; }

    // Navigation properties
    public ICollection<UserApplicationRole> UserApplicationRoles { get; set; } = new List<UserApplicationRole>();
}
