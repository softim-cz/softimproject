using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Infrastructure.Persistence.Seeding;

/// <summary>
/// Idempotent dev-only seeder. Fixed GUIDs let the FE and Playwright
/// rely on known entities. Never call from Production.
///
/// Depends on migrations having seeded:
///   - ProjectTemplate "Default" (00000000-0000-0000-0000-000000000001)
///   - TaskStates (Backlog/Todo/InProgress/Review/Done/Closed, 20000000-...)
///   - TicketPriorities (Low/Medium/High/Critical, 10000000-...)
/// We reuse those instead of inserting fresh copies.
/// </summary>
public sealed class DatabaseSeeder(ApplicationDbContext dbContext, ILogger<DatabaseSeeder> logger)
{
    public static class Ids
    {
        // Custom dev-seed GUIDs use the a* / b* / c* ... prefixes to avoid
        // colliding with migration-seeded IDs (which use 00/10/20 prefixes).
        public static readonly Guid AdminUser = new("a0000000-0000-0000-0000-000000000001");
        public static readonly Guid ManagerUser = new("a0000000-0000-0000-0000-000000000002");
        public static readonly Guid RegularUser = new("a0000000-0000-0000-0000-000000000003");
        public static readonly Guid ExternalUser = new("a0000000-0000-0000-0000-000000000004");

        public static readonly Guid AdminRole = new("b0000000-0000-0000-0000-000000000001");
        public static readonly Guid ManagerRole = new("b0000000-0000-0000-0000-000000000002");
        public static readonly Guid UserRole = new("b0000000-0000-0000-0000-000000000003");
        public static readonly Guid ExternalRole = new("b0000000-0000-0000-0000-000000000004");

        public static readonly Guid SoftimCompany = new("c0000000-0000-0000-0000-000000000001");
        public static readonly Guid DevelopmentType = new("c0000000-0000-0000-0000-000000000002");
        public static readonly Guid ActiveProjectState = new("c0000000-0000-0000-0000-000000000003");
        public static readonly Guid FeatureTaskType = new("c0000000-0000-0000-0000-000000000004");

        public static readonly Guid DemoProject = new("d0000000-0000-0000-0000-000000000001");
        public static readonly Guid DemoBoard = new("d0000000-0000-0000-0000-000000000020");
        public static readonly Guid ColumnTodo = new("d0000000-0000-0000-0000-000000000021");
        public static readonly Guid ColumnInProgress = new("d0000000-0000-0000-0000-000000000022");
        public static readonly Guid ColumnDone = new("d0000000-0000-0000-0000-000000000023");
        public static readonly Guid TicketOne = new("d0000000-0000-0000-0000-000000000011");
        public static readonly Guid TicketTwo = new("d0000000-0000-0000-0000-000000000012");

        // Migration-seeded GUIDs we reference (see comments above).
        public static readonly Guid DefaultTemplate = new("00000000-0000-0000-0000-000000000001");
        public static readonly Guid StateTodo = new("20000000-0000-0000-0000-000000000002");
        public static readonly Guid StateInProgress = new("20000000-0000-0000-0000-000000000003");
        public static readonly Guid StateDone = new("20000000-0000-0000-0000-000000000005");
        public static readonly Guid PriorityMedium = new("10000000-0000-0000-0000-000000000002");
        public static readonly Guid PriorityHigh = new("10000000-0000-0000-0000-000000000003");
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Seeding dev data");

        await SeedCompaniesAsync(cancellationToken);
        await SeedApplicationRolesAsync(cancellationToken);
        await SeedUsersAsync(cancellationToken);
        await SeedProjectLookupsAsync(cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        await SeedUserRolesAsync(cancellationToken);
        await SeedProjectAsync(cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        await SeedTicketsAsync(cancellationToken);
        await SeedKanbanBoardAsync(cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Dev data seeded");
    }

    private async Task SeedCompaniesAsync(CancellationToken ct)
    {
        if (await dbContext.Companies.AnyAsync(c => c.Id == Ids.SoftimCompany, ct)) return;
        dbContext.Companies.Add(new Company
        {
            Id = Ids.SoftimCompany,
            Name = "Softim",
            Description = "Internal dev seed",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
    }

    private async Task SeedApplicationRolesAsync(CancellationToken ct)
    {
        var existing = await dbContext.ApplicationRoles
            .Where(r => r.Id == Ids.AdminRole || r.Id == Ids.ManagerRole
                     || r.Id == Ids.UserRole || r.Id == Ids.ExternalRole)
            .Select(r => r.Id)
            .ToListAsync(ct);

        void Add(Guid id, string name, int order, bool full, bool write)
        {
            if (existing.Contains(id)) return;
            dbContext.ApplicationRoles.Add(new ApplicationRole
            {
                Id = id,
                Name = name,
                SortOrder = order,
                ProjectsCreate = full,
                ProjectsRead = true,
                ProjectsUpdate = full || write,
                ProjectsDelete = full,
                TimeTrackingCreate = true,
                TimeTrackingRead = true,
                TimeTrackingUpdate = full || write,
                TimeTrackingDelete = full || write,
                ReportsCreate = full || write,
                ReportsRead = true,
                ReportsUpdate = full,
                ReportsDelete = full,
                CreatedAt = DateTime.UtcNow
            });
        }

        Add(Ids.AdminRole, "Admin", 1, full: true, write: true);
        Add(Ids.ManagerRole, "Manager", 2, full: false, write: true);
        Add(Ids.UserRole, "User", 3, full: false, write: true);
        Add(Ids.ExternalRole, "External", 4, full: false, write: false);
    }

    private async Task SeedUsersAsync(CancellationToken ct)
    {
        var existing = await dbContext.Users
            .Where(u => u.Id == Ids.AdminUser || u.Id == Ids.ManagerUser
                     || u.Id == Ids.RegularUser || u.Id == Ids.ExternalUser)
            .Select(u => u.Id)
            .ToListAsync(ct);

        void Add(Guid id, string entraOid, string email, string display, string first, string last, GlobalRole role)
        {
            if (existing.Contains(id)) return;
            dbContext.Users.Add(new User
            {
                Id = id,
                EntraObjectId = entraOid,
                Email = email,
                DisplayName = display,
                FirstName = first,
                LastName = last,
                GlobalRole = role,
                IsActive = true,
                CompanyName = "Softim",
                CreatedAt = DateTime.UtcNow
            });
        }

        Add(Ids.AdminUser, "dev:admin", "admin@softim.cz", "Dev Admin", "Dev", "Admin", GlobalRole.Admin);
        Add(Ids.ManagerUser, "dev:manager", "manager@softim.cz", "Dev Manager", "Dev", "Manager", GlobalRole.Manager);
        Add(Ids.RegularUser, "dev:user", "user@softim.cz", "Dev User", "Dev", "User", GlobalRole.User);
        Add(Ids.ExternalUser, "dev:external", "external@softim.cz", "Dev External", "Dev", "External", GlobalRole.User);
    }

    private async Task SeedProjectLookupsAsync(CancellationToken ct)
    {
        if (!await dbContext.ProjectTypes.AnyAsync(t => t.Id == Ids.DevelopmentType, ct))
        {
            dbContext.ProjectTypes.Add(new ProjectType
            {
                Id = Ids.DevelopmentType,
                Name = "Development",
                SortOrder = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (!await dbContext.ProjectStates.AnyAsync(s => s.Id == Ids.ActiveProjectState, ct))
        {
            dbContext.ProjectStates.Add(new ProjectState
            {
                Id = Ids.ActiveProjectState,
                Name = "Active",
                Color = "#22c55e",
                SortOrder = 1,
                IsActive = true,
                IsDefault = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (!await dbContext.TaskTypes.AnyAsync(t => t.Id == Ids.FeatureTaskType, ct))
        {
            dbContext.TaskTypes.Add(new TaskType
            {
                Id = Ids.FeatureTaskType,
                Name = "Feature",
                SortOrder = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    private async Task SeedUserRolesAsync(CancellationToken ct)
    {
        var pairs = new[]
        {
            (Ids.AdminUser, Ids.AdminRole),
            (Ids.ManagerUser, Ids.ManagerRole),
            (Ids.RegularUser, Ids.UserRole),
            (Ids.ExternalUser, Ids.ExternalRole),
        };

        foreach (var (userId, roleId) in pairs)
        {
            var already = await dbContext.UserApplicationRoles
                .AnyAsync(uar => uar.UserId == userId && uar.ApplicationRoleId == roleId, ct);
            if (already) continue;
            dbContext.UserApplicationRoles.Add(new UserApplicationRole
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ApplicationRoleId = roleId
            });
        }
    }

    private async Task SeedProjectAsync(CancellationToken ct)
    {
        if (!await dbContext.Projects.AnyAsync(p => p.Id == Ids.DemoProject, ct))
        {
            dbContext.Projects.Add(new Project
            {
                Id = Ids.DemoProject,
                Name = "Demo Project",
                Code = "DEMO",
                Description = "Seeded dev project for local testing.",
                Status = ProjectStatus.Active,
                CompanyId = Ids.SoftimCompany,
                ProjectTypeId = Ids.DevelopmentType,
                ProjectStateId = Ids.ActiveProjectState,
                ProjectTemplateId = Ids.DefaultTemplate,
                NextTicketNumber = 3,
                CreatedAt = DateTime.UtcNow
            });
        }

        var memberships = new[]
        {
            (Ids.AdminUser, ProjectRole.ProjectManager),
            (Ids.ManagerUser, ProjectRole.ProjectManager),
            (Ids.RegularUser, ProjectRole.Developer),
            (Ids.ExternalUser, ProjectRole.Guest),
        };
        foreach (var (userId, role) in memberships)
        {
            var already = await dbContext.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == Ids.DemoProject && pm.UserId == userId, ct);
            if (already) continue;
            dbContext.ProjectMembers.Add(new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = Ids.DemoProject,
                UserId = userId,
                Role = role,
                JoinedAt = DateTime.UtcNow
            });
        }
    }

    private async Task SeedTicketsAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        if (!await dbContext.Tickets.AnyAsync(t => t.Id == Ids.TicketOne, ct))
        {
            dbContext.Tickets.Add(new Ticket
            {
                Id = Ids.TicketOne,
                ProjectId = Ids.DemoProject,
                Number = 1,
                Title = "Wire up demo flow",
                Description = "Seeded ticket used for FE/E2E smoke tests.",
                TicketPriorityId = Ids.PriorityMedium,
                TaskStateId = Ids.StateTodo,
                TaskTypeId = Ids.FeatureTaskType,
                ReporterId = Ids.AdminUser,
                AssigneeId = Ids.RegularUser,
                Position = 1000,
                CreatedAt = now
            });
        }

        if (!await dbContext.Tickets.AnyAsync(t => t.Id == Ids.TicketTwo, ct))
        {
            dbContext.Tickets.Add(new Ticket
            {
                Id = Ids.TicketTwo,
                ProjectId = Ids.DemoProject,
                Number = 2,
                Title = "Second seeded ticket",
                Description = "Covers multi-ticket scenarios.",
                TicketPriorityId = Ids.PriorityHigh,
                TaskStateId = Ids.StateInProgress,
                TaskTypeId = Ids.FeatureTaskType,
                ReporterId = Ids.ManagerUser,
                AssigneeId = Ids.ManagerUser,
                Position = 2000,
                CreatedAt = now
            });
        }

        if (!await dbContext.Comments.AnyAsync(c => c.TicketId == Ids.TicketOne, ct))
        {
            dbContext.Comments.Add(new Comment
            {
                Id = Guid.NewGuid(),
                TicketId = Ids.TicketOne,
                AuthorId = Ids.RegularUser,
                Content = "Seeded comment from Dev User.",
                IsInternal = true,
                Source = CommentSource.Manual,
                CreatedAt = now
            });
        }

        if (!await dbContext.Worklogs.AnyAsync(w => w.TicketId == Ids.TicketOne, ct))
        {
            dbContext.Worklogs.Add(new Worklog
            {
                Id = Guid.NewGuid(),
                ProjectId = Ids.DemoProject,
                TicketId = Ids.TicketOne,
                UserId = Ids.RegularUser,
                Date = DateOnly.FromDateTime(DateTime.UtcNow),
                Hours = 1.5m,
                Description = "Seeded worklog.",
                Source = WorklogSource.Manual,
                IsBillable = true,
                CreatedAt = now
            });
        }
    }

    private async Task SeedKanbanBoardAsync(CancellationToken ct)
    {
        if (await dbContext.KanbanBoards.AnyAsync(b => b.Id == Ids.DemoBoard, ct)) return;

        var todoState = await dbContext.TaskStates.FindAsync([Ids.StateTodo], ct);
        var inProgressState = await dbContext.TaskStates.FindAsync([Ids.StateInProgress], ct);
        var doneState = await dbContext.TaskStates.FindAsync([Ids.StateDone], ct);

        var board = new KanbanBoard
        {
            Id = Ids.DemoBoard,
            ProjectId = Ids.DemoProject,
            Name = "Demo Board",
            IsDefault = true,
            CreatedAt = DateTime.UtcNow,
            Columns =
            [
                new KanbanColumn
                {
                    Id = Ids.ColumnTodo,
                    Name = "Todo",
                    Position = 1,
                    Color = "#3b82f6",
                    CreatedAt = DateTime.UtcNow,
                    MapsToTaskStates = todoState is not null ? [todoState] : [],
                },
                new KanbanColumn
                {
                    Id = Ids.ColumnInProgress,
                    Name = "In Progress",
                    Position = 2,
                    Color = "#f59e0b",
                    CreatedAt = DateTime.UtcNow,
                    MapsToTaskStates = inProgressState is not null ? [inProgressState] : [],
                },
                new KanbanColumn
                {
                    Id = Ids.ColumnDone,
                    Name = "Done",
                    Position = 3,
                    Color = "#22c55e",
                    CreatedAt = DateTime.UtcNow,
                    MapsToTaskStates = doneState is not null ? [doneState] : [],
                },
            ],
        };

        dbContext.KanbanBoards.Add(board);
    }
}
