namespace SoftimProject.Domain.Entities;

public class UserApplicationRole
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ApplicationRoleId { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public ApplicationRole ApplicationRole { get; set; } = null!;
}
