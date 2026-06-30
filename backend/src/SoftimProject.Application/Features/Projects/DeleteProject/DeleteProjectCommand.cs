using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Projects.DeleteProject;

public sealed record DeleteProjectCommand(Guid Id) : IRequest, IRequireProjectAccess, IRequireRole
{
    public Guid ProjectId => Id;
    public string RequiredRole => "Admin";
}

public sealed class DeleteProjectCommandHandler(
    IApplicationDbContext dbContext) : IRequestHandler<DeleteProjectCommand>
{
    public async Task Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Project), request.Id);

        // Tickets, comments, views and saved filters reference the project with NoAction (to avoid
        // multiple cascade paths), so the database won't cascade them — a plain Remove(project)
        // fails on the FK for any non-empty project. Remove the whole graph explicitly and in
        // dependency order, deepest first. We delete descendants in code (not via DB cascade) so
        // the behavior is identical on SQL Server and on the in-memory provider used in tests.
        var ticketIds = await dbContext.Tickets
            .Where(t => t.ProjectId == project.Id)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        if (ticketIds.Count > 0)
        {
            dbContext.LinkedCommits.RemoveRange(await dbContext.LinkedCommits.Where(x => ticketIds.Contains(x.TicketId)).ToListAsync(cancellationToken));
            dbContext.LinkedPullRequests.RemoveRange(await dbContext.LinkedPullRequests.Where(x => ticketIds.Contains(x.TicketId)).ToListAsync(cancellationToken));
            dbContext.TicketWatchers.RemoveRange(await dbContext.TicketWatchers.Where(x => ticketIds.Contains(x.TicketId)).ToListAsync(cancellationToken));
            dbContext.TicketAttachments.RemoveRange(await dbContext.TicketAttachments.Where(x => ticketIds.Contains(x.TicketId)).ToListAsync(cancellationToken));
            dbContext.ChecklistItems.RemoveRange(await dbContext.ChecklistItems.Where(x => ticketIds.Contains(x.TicketId)).ToListAsync(cancellationToken));
            dbContext.Worklogs.RemoveRange(await dbContext.Worklogs.Where(x => ticketIds.Contains(x.TicketId)).ToListAsync(cancellationToken));
            dbContext.TicketCustomFieldValues.RemoveRange(await dbContext.TicketCustomFieldValues.Where(x => ticketIds.Contains(x.TicketId)).ToListAsync(cancellationToken));
        }

        // Comments link to both Ticket and Project; remove by project so ticket comments and
        // project-discussion comments both go.
        dbContext.Comments.RemoveRange(await dbContext.Comments.Where(c => c.ProjectId == project.Id).ToListAsync(cancellationToken));

        // Tickets themselves (load all + RemoveRange so EF orders self-referencing parent/child).
        dbContext.Tickets.RemoveRange(await dbContext.Tickets.Where(t => t.ProjectId == project.Id).ToListAsync(cancellationToken));

        // Project-scoped rows.
        dbContext.ViewConfigurations.RemoveRange(await dbContext.ViewConfigurations.Where(v => v.ProjectId == project.Id).ToListAsync(cancellationToken));
        dbContext.SavedFilters.RemoveRange(await dbContext.SavedFilters.Where(f => f.ProjectId == project.Id).ToListAsync(cancellationToken));
        dbContext.ProjectCustomFieldValues.RemoveRange(await dbContext.ProjectCustomFieldValues.Where(v => v.ProjectId == project.Id).ToListAsync(cancellationToken));
        dbContext.AiReports.RemoveRange(await dbContext.AiReports.Where(r => r.ProjectId == project.Id).ToListAsync(cancellationToken));
        dbContext.SyncLogs.RemoveRange(await dbContext.SyncLogs.Where(s => s.ProjectId == project.Id).ToListAsync(cancellationToken));
        dbContext.ProjectMembers.RemoveRange(await dbContext.ProjectMembers.Where(m => m.ProjectId == project.Id).ToListAsync(cancellationToken));

        var boardIds = await dbContext.KanbanBoards.Where(b => b.ProjectId == project.Id).Select(b => b.Id).ToListAsync(cancellationToken);
        if (boardIds.Count > 0)
            dbContext.KanbanColumns.RemoveRange(await dbContext.KanbanColumns.Where(c => boardIds.Contains(c.BoardId)).ToListAsync(cancellationToken));
        dbContext.KanbanBoards.RemoveRange(await dbContext.KanbanBoards.Where(b => b.ProjectId == project.Id).ToListAsync(cancellationToken));

        // Sub-projects reference this one as parent (Restrict) — detach them so the delete doesn't fail.
        var subProjects = await dbContext.Projects.Where(p => p.ParentProjectId == project.Id).ToListAsync(cancellationToken);
        foreach (var sub in subProjects) sub.ParentProjectId = null;

        dbContext.Projects.Remove(project);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
