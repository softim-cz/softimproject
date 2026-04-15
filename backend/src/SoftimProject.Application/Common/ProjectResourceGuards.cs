using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Application.Common;

public static class ProjectResourceGuards
{
    public static async Task<Ticket> GetTicketForProjectAsync(
        this IApplicationDbContext dbContext,
        Guid projectId,
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Tickets
            .FirstOrDefaultAsync(t => t.Id == ticketId && t.ProjectId == projectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Ticket), ticketId);
    }

    public static async Task<Comment> GetTicketCommentForProjectAsync(
        this IApplicationDbContext dbContext,
        Guid projectId,
        Guid ticketId,
        Guid commentId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Comments
            .Include(c => c.Ticket)
            .FirstOrDefaultAsync(
                c => c.Id == commentId
                    && c.TicketId == ticketId
                    && c.Ticket != null
                    && c.Ticket.ProjectId == projectId,
                cancellationToken)
            ?? throw new NotFoundException(nameof(Comment), commentId);
    }

    public static async Task<TicketAttachment> GetTicketAttachmentForProjectAsync(
        this IApplicationDbContext dbContext,
        Guid projectId,
        Guid ticketId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        return await dbContext.TicketAttachments
            .Include(a => a.Ticket)
            .FirstOrDefaultAsync(
                a => a.Id == attachmentId
                    && a.TicketId == ticketId
                    && a.Ticket.ProjectId == projectId,
                cancellationToken)
            ?? throw new NotFoundException(nameof(TicketAttachment), attachmentId);
    }

    public static async Task<ChecklistItem> GetChecklistItemForProjectAsync(
        this IApplicationDbContext dbContext,
        Guid projectId,
        Guid ticketId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        return await dbContext.ChecklistItems
            .Include(ci => ci.Ticket)
            .FirstOrDefaultAsync(
                ci => ci.Id == itemId
                    && ci.TicketId == ticketId
                    && ci.Ticket.ProjectId == projectId,
                cancellationToken)
            ?? throw new NotFoundException(nameof(ChecklistItem), itemId);
    }

    public static async Task<Worklog> GetWorklogForProjectAsync(
        this IApplicationDbContext dbContext,
        Guid projectId,
        Guid worklogId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Worklogs
            .FirstOrDefaultAsync(w => w.Id == worklogId && w.ProjectId == projectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Worklog), worklogId);
    }
}
