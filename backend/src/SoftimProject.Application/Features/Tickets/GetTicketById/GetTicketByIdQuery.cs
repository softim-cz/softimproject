using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Tickets.GetTicketById;

public sealed record TicketCommentDto(
    Guid Id,
    Guid AuthorId,
    string AuthorDisplayName,
    string Content,
    bool IsInternal,
    CommentSource Source,
    DateTime CreatedAt);

public sealed record TicketAttachmentDto(
    Guid Id,
    string FileName,
    string BlobUrl,
    string ContentType,
    long FileSizeBytes,
    Guid UploadedById,
    DateTime CreatedAt);

public sealed record TicketChecklistItemDto(
    Guid Id,
    string Text,
    bool IsCompleted,
    int Position);

public sealed record TicketDetailDto(
    Guid Id,
    Guid ProjectId,
    Guid? ColumnId,
    string Title,
    string? Description,
    TicketPriority Priority,
    TicketStatus Status,
    double Position,
    Guid? AssigneeId,
    string? AssigneeDisplayName,
    Guid ReporterId,
    string ReporterDisplayName,
    DateOnly? DueDate,
    decimal? EstimatedHours,
    string? AiSummary,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    List<TicketCommentDto> Comments,
    List<TicketAttachmentDto> Attachments,
    List<TicketChecklistItemDto> ChecklistItems);

public sealed record GetTicketByIdQuery(Guid ProjectId, Guid TicketId) : IRequest<TicketDetailDto>, IRequireProjectAccess;

public sealed class GetTicketByIdQueryHandler(
    IApplicationDbContext dbContext) : IRequestHandler<GetTicketByIdQuery, TicketDetailDto>
{
    public async Task<TicketDetailDto> Handle(GetTicketByIdQuery request, CancellationToken cancellationToken)
    {
        var ticket = await dbContext.Tickets
            .Include(t => t.Assignee)
            .Include(t => t.Reporter)
            .Include(t => t.Comments.OrderByDescending(c => c.CreatedAt)).ThenInclude(c => c.Author)
            .Include(t => t.Attachments.OrderByDescending(a => a.CreatedAt))
            .Include(t => t.ChecklistItems.OrderBy(ci => ci.Position))
            .FirstOrDefaultAsync(t => t.Id == request.TicketId && t.ProjectId == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Ticket), request.TicketId);

        return new TicketDetailDto(
            ticket.Id,
            ticket.ProjectId,
            ticket.ColumnId,
            ticket.Title,
            ticket.Description,
            ticket.Priority,
            ticket.Status,
            ticket.Position,
            ticket.AssigneeId,
            ticket.Assignee?.DisplayName,
            ticket.ReporterId,
            ticket.Reporter.DisplayName,
            ticket.DueDate,
            ticket.EstimatedHours,
            ticket.AiSummary,
            ticket.CreatedAt,
            ticket.UpdatedAt,
            ticket.Comments.Select(c => new TicketCommentDto(
                c.Id, c.AuthorId, c.Author.DisplayName, c.Content, c.IsInternal, c.Source, c.CreatedAt)).ToList(),
            ticket.Attachments.Select(a => new TicketAttachmentDto(
                a.Id, a.FileName, a.BlobUrl, a.ContentType, a.FileSizeBytes, a.UploadedById, a.CreatedAt)).ToList(),
            ticket.ChecklistItems.Select(ci => new TicketChecklistItemDto(
                ci.Id, ci.Text, ci.IsCompleted, ci.Position)).ToList());
    }
}
