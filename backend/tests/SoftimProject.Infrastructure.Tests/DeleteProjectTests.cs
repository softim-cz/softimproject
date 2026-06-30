using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Features.Projects.DeleteProject;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;
using SoftimProject.Infrastructure.Persistence;

namespace SoftimProject.Infrastructure.Tests;

public class DeleteProjectTests
{
    [Fact]
    public async Task Deletes_Project_With_All_Tickets_Comments_Worklogs_And_Children()
    {
        await using var db = CreateDbContext();
        var seed = await SeedAsync(db);

        var handler = new DeleteProjectCommandHandler(db);
        await handler.Handle(new DeleteProjectCommand(seed.ProjectId), CancellationToken.None);

        (await db.Projects.AnyAsync(p => p.Id == seed.ProjectId)).Should().BeFalse();
        (await db.Tickets.AnyAsync(t => t.ProjectId == seed.ProjectId)).Should().BeFalse();
        (await db.Comments.AnyAsync(c => c.ProjectId == seed.ProjectId)).Should().BeFalse();
        (await db.Worklogs.AnyAsync(w => w.Id == seed.WorklogId)).Should().BeFalse();
        (await db.ChecklistItems.AnyAsync(ci => ci.Id == seed.ChecklistId)).Should().BeFalse();
        (await db.TicketCustomFieldValues.AnyAsync(v => v.TicketId == seed.TicketId)).Should().BeFalse();
        (await db.KanbanBoards.AnyAsync(b => b.ProjectId == seed.ProjectId)).Should().BeFalse();
        (await db.KanbanColumns.AnyAsync(c => c.Id == seed.ColumnId)).Should().BeFalse();
        (await db.ProjectMembers.AnyAsync(m => m.ProjectId == seed.ProjectId)).Should().BeFalse();
        (await db.SyncLogs.AnyAsync(s => s.ProjectId == seed.ProjectId)).Should().BeFalse();
    }

    [Fact]
    public async Task Detaches_SubProjects_Instead_Of_Failing()
    {
        await using var db = CreateDbContext();
        var seed = await SeedAsync(db);
        var sub = new Project { Id = Guid.NewGuid(), Name = "Sub", Code = "SUB", Status = ProjectStatus.Active, ProjectTemplateId = seed.TemplateId, ParentProjectId = seed.ProjectId, CreatedAt = DateTime.UtcNow };
        db.Projects.Add(sub);
        await db.SaveChangesAsync();

        var handler = new DeleteProjectCommandHandler(db);
        await handler.Handle(new DeleteProjectCommand(seed.ProjectId), CancellationToken.None);

        var reloaded = await db.Projects.FirstAsync(p => p.Id == sub.Id);
        reloaded.ParentProjectId.Should().BeNull();
    }

    private static ApplicationDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task<SeedResult> SeedAsync(ApplicationDbContext db)
    {
        var owner = new User { Id = Guid.NewGuid(), EntraObjectId = Guid.NewGuid().ToString(), Email = "o@x.local", DisplayName = "Owner", GlobalRole = GlobalRole.User, IsActive = true, CreatedAt = DateTime.UtcNow };
        var template = new ProjectTemplate { Id = Guid.NewGuid(), Name = $"T-{Guid.NewGuid():N}", IsActive = true, CreatedAt = DateTime.UtcNow };
        var project = new Project { Id = Guid.NewGuid(), Name = "P", Code = "PRJ", Status = ProjectStatus.Active, ProjectTemplateId = template.Id, CreatedAt = DateTime.UtcNow };
        var state = new TaskState { Id = Guid.NewGuid(), Name = "Todo", Color = "#fff", SortOrder = 1, IsActive = true, IsDefault = true, ProjectTemplateId = template.Id, CreatedAt = DateTime.UtcNow };
        var prio = new TicketPriority { Id = Guid.NewGuid(), Name = "Normal", Color = "#0f0", SortOrder = 1, IsActive = true, IsDefault = true, ProjectTemplateId = template.Id, CreatedAt = DateTime.UtcNow };
        var ticket = new Ticket { Id = Guid.NewGuid(), ProjectId = project.Id, Number = 1, Title = "T1", TicketPriorityId = prio.Id, TaskStateId = state.Id, Position = 1, ReporterId = owner.Id, CreatedAt = DateTime.UtcNow };
        var board = new KanbanBoard { Id = Guid.NewGuid(), ProjectId = project.Id, Name = "Main", IsDefault = true, CreatedAt = DateTime.UtcNow };
        var column = new KanbanColumn { Id = Guid.NewGuid(), BoardId = board.Id, Name = "Todo", Position = 0, CreatedAt = DateTime.UtcNow };
        var member = new ProjectMember { Id = Guid.NewGuid(), ProjectId = project.Id, UserId = owner.Id, Role = ProjectRole.ProjectManager, JoinedAt = DateTime.UtcNow };
        var comment = new Comment { Id = Guid.NewGuid(), TicketId = ticket.Id, ProjectId = project.Id, AuthorId = owner.Id, Content = "c", Source = CommentSource.Manual, CreatedAt = DateTime.UtcNow };
        var worklog = new Worklog { Id = Guid.NewGuid(), TicketId = ticket.Id, UserId = owner.Id, Hours = 1m, Date = new DateOnly(2026, 6, 1), Description = "w", IsBillable = true, Source = WorklogSource.Manual, CreatedAt = DateTime.UtcNow };
        var checklist = new ChecklistItem { Id = Guid.NewGuid(), TicketId = ticket.Id, Text = "ci", IsCompleted = false, Position = 0, CreatedAt = DateTime.UtcNow };
        var def = new CustomFieldDefinition { Id = Guid.NewGuid(), Name = "Severity", FieldType = CustomFieldType.Text, AppliesTo = "Ticket", IsActive = true, CreatedAt = DateTime.UtcNow };
        var cfv = new TicketCustomFieldValue { Id = Guid.NewGuid(), TicketId = ticket.Id, CustomFieldDefinitionId = def.Id, Value = "High", CreatedAt = DateTime.UtcNow };
        var syncLog = new SyncLog { Id = Guid.NewGuid(), ProjectId = project.Id, SyncType = SyncType.EasyProject, Status = SyncStatus.Success, StartedAt = DateTime.UtcNow, CompletedAt = DateTime.UtcNow };

        db.Users.Add(owner);
        db.ProjectTemplates.Add(template);
        db.Projects.Add(project);
        db.TaskStates.Add(state);
        db.TicketPriorities.Add(prio);
        db.Tickets.Add(ticket);
        db.KanbanBoards.Add(board);
        db.KanbanColumns.Add(column);
        db.ProjectMembers.Add(member);
        db.Comments.Add(comment);
        db.Worklogs.Add(worklog);
        db.ChecklistItems.Add(checklist);
        db.CustomFieldDefinitions.Add(def);
        db.TicketCustomFieldValues.Add(cfv);
        db.SyncLogs.Add(syncLog);
        await db.SaveChangesAsync();

        return new SeedResult(template.Id, project.Id, ticket.Id, worklog.Id, checklist.Id, column.Id);
    }

    private sealed record SeedResult(Guid TemplateId, Guid ProjectId, Guid TicketId, Guid WorklogId, Guid ChecklistId, Guid ColumnId);
}
