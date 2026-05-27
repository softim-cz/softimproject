using System.Linq.Expressions;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Tickets;

public sealed record TicketPersonDto(
    Guid Id,
    string DisplayName);

public sealed record TicketCommentDto(
    Guid Id,
    TicketPersonDto Author,
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

public sealed record TicketSubTicketDto(
    Guid Id,
    int Number,
    string Key,
    string Title,
    Guid TaskStateId,
    string TaskStateName,
    string TaskStateColor);

public sealed record TicketDetailDto(
    Guid Id,
    int Number,
    string Key,
    Guid ProjectId,
    Guid? ColumnId,
    string Title,
    string? Description,
    Guid TicketPriorityId,
    string TicketPriorityName,
    string TicketPriorityColor,
    Guid TaskStateId,
    string TaskStateName,
    string TaskStateColor,
    double Position,
    Guid? AssigneeId,
    TicketPersonDto? Assignee,
    Guid ReporterId,
    TicketPersonDto Reporter,
    DateOnly? DueDate,
    decimal? EstimatedHours,
    Guid? TaskTypeId,
    string? TaskTypeName,
    string? TaskTypeIcon,
    Guid? ParentTicketId,
    int? ParentTicketNumber,
    string? ParentTicketKey,
    string? ParentTicketTitle,
    decimal CumulativeWorkedHours,
    decimal? ExternalBudget,
    string? ExternalUser,
    string? ExternalId,
    string? ExternalUrl,
    string? ImplementationNotes,
    string? LastComment,
    string? AiSummary,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    List<TicketCommentDto> Comments,
    List<TicketAttachmentDto> Attachments,
    List<TicketChecklistItemDto> ChecklistItems,
    List<TicketSubTicketDto> SubTickets);

internal static class TicketDetailProjections
{
    public static readonly Expression<Func<Ticket, TicketDetailDto>> Detail = ticket => new TicketDetailDto(
        ticket.Id,
        ticket.Number,
        ticket.Project.Code + "-" + ticket.Number,
        ticket.ProjectId,
        ticket.ColumnId,
        ticket.Title,
        ticket.Description,
        ticket.TicketPriorityId,
        ticket.TicketPriority.Name,
        ticket.TicketPriority.Color,
        ticket.TaskStateId,
        ticket.TaskState.Name,
        ticket.TaskState.Color,
        ticket.Position,
        ticket.AssigneeId,
        ticket.Assignee != null ? new TicketPersonDto(ticket.Assignee.Id, ticket.Assignee.DisplayName) : null,
        ticket.ReporterId,
        new TicketPersonDto(ticket.Reporter.Id, ticket.Reporter.DisplayName),
        ticket.DueDate,
        ticket.EstimatedHours,
        ticket.TaskTypeId,
        ticket.TaskType != null ? ticket.TaskType.Name : null,
        ticket.TaskType != null ? ticket.TaskType.Icon : null,
        ticket.ParentTicketId,
        ticket.ParentTicket != null ? ticket.ParentTicket.Number : (int?)null,
        ticket.ParentTicket != null ? ticket.ParentTicket.Project.Code + "-" + ticket.ParentTicket.Number : null,
        ticket.ParentTicket != null ? ticket.ParentTicket.Title : null,
        ticket.CumulativeWorkedHours,
        ticket.ExternalBudget,
        ticket.ExternalUser,
        ticket.ExternalId,
        ticket.ExternalUrl,
        ticket.ImplementationNotes,
        ticket.LastComment,
        ticket.AiSummary,
        ticket.CreatedAt,
        ticket.UpdatedAt,
        ticket.Comments
            .OrderByDescending(comment => comment.CreatedAt)
            .Select(comment => new TicketCommentDto(
                comment.Id,
                new TicketPersonDto(comment.Author.Id, comment.Author.DisplayName),
                comment.Content,
                comment.IsInternal,
                comment.Source,
                comment.CreatedAt))
            .ToList(),
        ticket.Attachments
            .OrderByDescending(attachment => attachment.CreatedAt)
            .Select(attachment => new TicketAttachmentDto(
                attachment.Id,
                attachment.FileName,
                attachment.BlobUrl,
                attachment.ContentType,
                attachment.FileSizeBytes,
                attachment.UploadedById,
                attachment.CreatedAt))
            .ToList(),
        ticket.ChecklistItems
            .OrderBy(checklistItem => checklistItem.Position)
            .Select(checklistItem => new TicketChecklistItemDto(
                checklistItem.Id,
                checklistItem.Text,
                checklistItem.IsCompleted,
                checklistItem.Position))
            .ToList(),
        ticket.SubTickets
            .OrderBy(sub => sub.Number)
            .Select(sub => new TicketSubTicketDto(
                sub.Id,
                sub.Number,
                ticket.Project.Code + "-" + sub.Number,
                sub.Title,
                sub.TaskStateId,
                sub.TaskState.Name,
                sub.TaskState.Color))
            .ToList());
}
