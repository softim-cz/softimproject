using MediatR;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Export;

public sealed record ExportColumn(string Field, string Header);

public sealed record ExportXlsxQuery(
    Guid ProjectId,
    string ViewType,
    List<ExportColumn> Columns) : IRequest<byte[]>;

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
            var row = 2;
            await foreach (var ticket in dbContext.Tickets
                               .AsNoTracking()
                               .Where(t => t.ProjectId == request.ProjectId)
                               .OrderBy(t => t.Position)
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
            var row = 2;
            await foreach (var worklog in dbContext.Worklogs
                               .AsNoTracking()
                               .Where(w => w.ProjectId == request.ProjectId)
                               .OrderByDescending(w => w.Date)
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
