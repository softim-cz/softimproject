using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;
using SoftimProject.Infrastructure.Persistence;

namespace SoftimProject.WebApi.Tests.Integration;

// Cross-project membership matrix used by authorization boundary tests:
//   UserA = Developer on ProjectA, PmA = ProjectManager on ProjectA, GuestA = Guest on ProjectA
//   UserB = Developer on ProjectB (cross-project isolation)
// Admin is seeded first so the auto-provisioning escalation in
// CurrentUserService.InitializeAsync can't accidentally promote test users.
public static class TestDataSeeder
{
    public const string AdminOid = "dev:admin";
    public const string UserAOid = "dev:user-a";
    public const string UserBOid = "dev:user-b";
    public const string PmAOid = "dev:pm-a";
    public const string GuestAOid = "dev:guest-a";

    public static readonly Guid AdminId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid UserAId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    public static readonly Guid UserBId = Guid.Parse("10000000-0000-0000-0000-000000000003");
    public static readonly Guid PmAId = Guid.Parse("10000000-0000-0000-0000-000000000004");
    public static readonly Guid GuestAId = Guid.Parse("10000000-0000-0000-0000-000000000005");
    public static readonly Guid ProjectAId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    public static readonly Guid ProjectBId = Guid.Parse("20000000-0000-0000-0000-000000000002");
    public static readonly Guid ProjectABoardId = Guid.Parse("30000000-0000-0000-0000-000000000001");

    public static async Task SeedAsync(ApplicationDbContext db)
    {
        db.Users.AddRange(
            new User
            {
                Id = AdminId,
                EntraObjectId = AdminOid,
                Email = "admin@test",
                DisplayName = "Admin",
                GlobalRole = GlobalRole.Admin,
                IsActive = true,
            },
            new User
            {
                Id = UserAId,
                EntraObjectId = UserAOid,
                Email = "user-a@test",
                DisplayName = "User A",
                GlobalRole = GlobalRole.User,
                IsActive = true,
            },
            new User
            {
                Id = UserBId,
                EntraObjectId = UserBOid,
                Email = "user-b@test",
                DisplayName = "User B",
                GlobalRole = GlobalRole.User,
                IsActive = true,
            },
            new User
            {
                Id = PmAId,
                EntraObjectId = PmAOid,
                Email = "pm-a@test",
                DisplayName = "PM A",
                GlobalRole = GlobalRole.User,
                IsActive = true,
            },
            new User
            {
                Id = GuestAId,
                EntraObjectId = GuestAOid,
                Email = "guest-a@test",
                DisplayName = "Guest A",
                GlobalRole = GlobalRole.User,
                IsActive = true,
            });

        db.Projects.AddRange(
            new Project
            {
                Id = ProjectAId,
                Name = "Project A",
                Code = "PRJA",
                Status = ProjectStatus.Active,
            },
            new Project
            {
                Id = ProjectBId,
                Name = "Project B",
                Code = "PRJB",
                Status = ProjectStatus.Active,
            });

        db.ProjectMembers.AddRange(
            new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = ProjectAId,
                UserId = UserAId,
                Role = ProjectRole.Developer,
                JoinedAt = DateTime.UtcNow,
            },
            new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = ProjectAId,
                UserId = PmAId,
                Role = ProjectRole.ProjectManager,
                JoinedAt = DateTime.UtcNow,
            },
            new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = ProjectAId,
                UserId = GuestAId,
                Role = ProjectRole.Guest,
                JoinedAt = DateTime.UtcNow,
            },
            new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = ProjectBId,
                UserId = UserBId,
                Role = ProjectRole.Developer,
                JoinedAt = DateTime.UtcNow,
            });

        // Board for ProjectA used by role-matrix tests that target PUT /boards/{id}.
        db.KanbanBoards.Add(new KanbanBoard
        {
            Id = ProjectABoardId,
            ProjectId = ProjectAId,
            Name = "Main Board",
            IsDefault = true,
            CreatedAt = DateTime.UtcNow,
        });

        await db.SaveChangesAsync();
    }
}
