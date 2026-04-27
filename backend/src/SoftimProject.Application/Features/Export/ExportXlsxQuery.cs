using MediatR;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Application.Features.Export;

public sealed record ExportColumn(string Field, string Header);

public sealed record ExportXlsxQuery(
    Guid ProjectId,
    string ViewType,
    List<ExportColumn> Columns,
    string? SearchTerm = null,
    string? TaskStateName = null,
    string? TicketPriorityName = null,
    string? AssigneeName = null,
    string? TaskTypeName = null,
    DateOnly? DueDate = null,
    string? SortField = null,
    string? SortDirection = null) : IRequest<byte[]>, IRequireProjectAccess;

public sealed class ExportXlsxQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<ExportXlsxQuery, byte[]>
{
    public async Task<byte[]> Handle(ExportXlsxQuery request, CancellationToken cancellationToken)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add(request.ViewType);

        for (var i = 0; i < request.Columns.Count; i++)
        {
            worksheet.Cells[1, i + 1].Value = request.Columns[i].Header;
            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
        }

        if (request.ViewType == "TaskList")
        {
            var query = dbContext.Tickets
                .AsNoTracking()
                .Where(t => t.ProjectId == request.ProjectId);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                query = query.Where(t => t.Title.Contains(request.SearchTerm));

            if (!string.IsNullOrWhiteSpace(request.TaskStateName))
                query = query.Where(t => t.TaskState.Name == request.TaskStateName);

            if (!string.IsNullOrWhiteSpace(request.TicketPriorityName))
                query = query.Where(t => t.TicketPriority.Name == request.TicketPriorityName);

            if (!string.IsNullOrWhiteSpace(request.AssigneeName))
                query = query.Where(t => t.Assignee != null && t.Assignee.DisplayName == request.AssigneeName);

            if (!string.IsNullOrWhiteSpace(request.TaskTypeName))
                query = query.Where(t => t.TaskType != null && t.TaskType.Name == request.TaskTypeName);

            if (request.DueDate.HasValue)
                query = query.Where(t => t.DueDate == request.DueDate.Value);

            var ordered = ApplyTicketSort(query, request.SortField, request.SortDirection);

            var row = 2;
            await foreach (var ticket in ordered
                               .Select(t => new TicketExportRow(
                                   t.Project.Code + "-" + t.Number,
                                   t.Title,
                                   t.TaskState.Name,
                                   t.TicketPriority.Name,
                                   t.Assignee != null ? t.Assignee.DisplayName : string.Empty,
                                   t.TaskType != null ? t.TaskType.Name : string.Empty,
                                   t.DueDate,
                                   t.EstimatedHours,
                                   t.CumulativeWorkedHours,
                                   t.Description,
                                   t.CreatedAt))
                               .AsAsyncEnumerable()
                               .WithCancellation(cancellationToken))
            {
                for (var col = 0; col < request.Columns.Count; col++)
                {
                    worksheet.Cells[row, col + 1].Value = GetTicketFieldValue(ticket, request.Columns[col].Field);
                }

                row++;
            }
        }
        else if (request.ViewType == "Worklogs")
        {
            var query = dbContext.Worklogs
                .AsNoTracking()
                .Where(w => w.Ticket.ProjectId == request.ProjectId);

            if (!string.IsNullOrWhiteSpace(request.AssigneeName))
                query = query.Where(w => w.User.DisplayName == request.AssigneeName);

            var ordered = query.OrderByDescending(w => w.Date);

            var row = 2;
            await foreach (var worklog in ordered
                               .Select(w => new WorklogExportRow(
                                   w.Date,
                                   w.User.DisplayName,
                                   w.Hours,
                                   w.Description,
                                   w.Source.ToString(),
                                   w.IsBillable,
                                   w.Invoiced))
                               .AsAsyncEnumerable()
                               .WithCancellation(cancellationToken))
            {
                for (var col = 0; col < request.Columns.Count; col++)
                {
                    worksheet.Cells[row, col + 1].Value = GetWorklogFieldValue(worklog, request.Columns[col].Field);
                }

                row++;
            }
        }

        worksheet.Cells.AutoFitColumns();
        return await package.GetAsByteArrayAsync(cancellationToken);
    }

    private static IQueryable<Ticket> ApplyTicketSort(
        IQueryable<Ticket> query,
        string? sortField,
        string? sortDirection)
    {
        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return sortField switch
        {
            "key" or "number" => descending ? query.OrderByDescending(t => t.Number) : query.OrderBy(t => t.Number),
            "title" => descending ? query.OrderByDescending(t => t.Title) : query.OrderBy(t => t.Title),
            "taskStateName" => descending
                ? query.OrderByDescending(t => t.TaskState.Name)
                : query.OrderBy(t => t.TaskState.Name),
            "ticketPriorityName" => descending
                ? query.OrderByDescending(t => t.TicketPriority.Name)
                : query.OrderBy(t => t.TicketPriority.Name),
            "assignee" => descending
                ? query.OrderByDescending(t => t.Assignee != null ? t.Assignee.DisplayName : string.Empty)
                : query.OrderBy(t => t.Assignee != null ? t.Assignee.DisplayName : string.Empty),
            "dueDate" => descending ? query.OrderByDescending(t => t.DueDate) : query.OrderBy(t => t.DueDate),
            "estimatedHours" => descending
                ? query.OrderByDescending(t => t.EstimatedHours)
                : query.OrderBy(t => t.EstimatedHours),
            "createdAt" => descending
                ? query.OrderByDescending(t => t.CreatedAt)
                : query.OrderBy(t => t.CreatedAt),
            _ => query.OrderBy(t => t.Position),
        };
    }

    private static object? GetTicketFieldValue(TicketExportRow ticket, string field) => field switch
    {
        "key" => ticket.Key,
        "title" => ticket.Title,
        "taskStateName" => ticket.TaskStateName,
        "status" => ticket.TaskStateName,
        "ticketPriorityName" => ticket.TicketPriorityName,
        "priority" => ticket.TicketPriorityName,
        "assignee" => ticket.Assignee,
        "taskTypeName" => ticket.TaskTypeName,
        "taskType" => ticket.TaskTypeName,
        "dueDate" => ticket.DueDate?.ToString("yyyy-MM-dd") ?? string.Empty,
        "estimatedHours" => ticket.EstimatedHours,
        "cumulativeWorkedHours" => ticket.CumulativeWorkedHours,
        "description" => ticket.Description ?? string.Empty,
        "createdAt" => ticket.CreatedAt.ToString("yyyy-MM-dd"),
        _ => string.Empty
    };

    private static object? GetWorklogFieldValue(WorklogExportRow worklog, string field) => field switch
    {
        "date" => worklog.Date.ToString("yyyy-MM-dd"),
        "user" => worklog.UserDisplayName,
        "hours" => worklog.Hours,
        "description" => worklog.Description ?? string.Empty,
        "source" => worklog.Source,
        "isBillable" => worklog.IsBillable ? "Yes" : "No",
        "invoiced" => worklog.Invoiced ?? string.Empty,
        _ => string.Empty
    };

    private sealed record TicketExportRow(
        string Key,
        string Title,
        string TaskStateName,
        string TicketPriorityName,
        string Assignee,
        string TaskTypeName,
        DateOnly? DueDate,
        decimal? EstimatedHours,
        decimal CumulativeWorkedHours,
        string? Description,
        DateTime CreatedAt);

    private sealed record WorklogExportRow(
        DateOnly Date,
        string UserDisplayName,
        decimal Hours,
        string? Description,
        string Source,
        bool IsBillable,
        string? Invoiced);
}
