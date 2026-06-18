using System.Linq.Expressions;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Application.Common;

// Shared, server-side ticket ordering used by the ticket list and the xlsx export.
// Field names match the frontend table column ids so the client can pass the active
// sort column straight through.
public static class TicketSort
{
    // Applies a known sort column, or returns null when sortField is empty/unknown
    // so the caller can fall back to its own default ordering.
    public static IOrderedQueryable<Ticket>? TryApply(
        IQueryable<Ticket> query, string? sortField, string? sortDirection)
    {
        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return sortField switch
        {
            "key" or "number" => Order(query, t => t.Number, descending),
            "title" => Order(query, t => t.Title, descending),
            "taskStateName" or "status" => Order(query, t => t.TaskState.Name, descending),
            "ticketPriorityName" or "priority" => Order(query, t => t.TicketPriority.Name, descending),
            "assignee" => Order(query, t => t.Assignee != null ? t.Assignee.DisplayName : string.Empty, descending),
            "taskTypeName" => Order(query, t => t.TaskType != null ? t.TaskType.Name : string.Empty, descending),
            "dueDate" => Order(query, t => t.DueDate, descending),
            "estimatedHours" => Order(query, t => t.EstimatedHours, descending),
            "cumulativeWorkedHours" => Order(query, t => t.CumulativeWorkedHours, descending),
            "createdAt" => Order(query, t => t.CreatedAt, descending),
            _ => null,
        };
    }

    private static IOrderedQueryable<Ticket> Order<TKey>(
        IQueryable<Ticket> query, Expression<Func<Ticket, TKey>> key, bool descending)
        => descending ? query.OrderByDescending(key) : query.OrderBy(key);
}
