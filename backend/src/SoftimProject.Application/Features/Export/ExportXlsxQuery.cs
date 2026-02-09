using MediatR;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Export;

public sealed record ExportColumn(string Field, string Header);

public sealed record ExportXlsxQuery(
    Guid ProjectId,
    string ViewType,        // "TaskList" or "Worklogs"
    List<ExportColumn> Columns) : IRequest<byte[]>;

public sealed class ExportXlsxQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<ExportXlsxQuery, byte[]>
{
    public async Task<byte[]> Handle(ExportXlsxQuery request, CancellationToken cancellationToken)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add(request.ViewType);

        // Headers
        for (int i = 0; i < request.Columns.Count; i++)
        {
            worksheet.Cells[1, i + 1].Value = request.Columns[i].Header;
            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
        }

        if (request.ViewType == "TaskList")
        {
            var tickets = await dbContext.Tickets
                .Include(t => t.Assignee)
                .Include(t => t.TaskType)
                .Include(t => t.TaskState)
                .Where(t => t.ProjectId == request.ProjectId)
                .OrderBy(t => t.Position)
                .ToListAsync(cancellationToken);

            for (int row = 0; row < tickets.Count; row++)
            {
                var ticket = tickets[row];
                for (int col = 0; col < request.Columns.Count; col++)
                {
                    worksheet.Cells[row + 2, col + 1].Value = GetTicketFieldValue(ticket, request.Columns[col].Field);
                }
            }
        }
        else if (request.ViewType == "Worklogs")
        {
            var worklogs = await dbContext.Worklogs
                .Include(w => w.User)
                .Where(w => w.ProjectId == request.ProjectId)
                .OrderByDescending(w => w.Date)
                .ToListAsync(cancellationToken);

            for (int row = 0; row < worklogs.Count; row++)
            {
                var worklog = worklogs[row];
                for (int col = 0; col < request.Columns.Count; col++)
                {
                    worksheet.Cells[row + 2, col + 1].Value = GetWorklogFieldValue(worklog, request.Columns[col].Field);
                }
            }
        }

        worksheet.Cells.AutoFitColumns();
        return await package.GetAsByteArrayAsync(cancellationToken);
    }

    private static object? GetTicketFieldValue(Domain.Entities.Ticket ticket, string field) => field switch
    {
        "title" => ticket.Title,
        "status" => ticket.Status.ToString(),
        "priority" => ticket.Priority.ToString(),
        "assignee" => ticket.Assignee?.DisplayName ?? "",
        "taskType" => ticket.TaskType?.Name ?? "",
        "taskState" => ticket.TaskState?.Name ?? "",
        "dueDate" => ticket.DueDate?.ToString("yyyy-MM-dd") ?? "",
        "estimatedHours" => ticket.EstimatedHours,
        "cumulativeWorkedHours" => ticket.CumulativeWorkedHours,
        "description" => ticket.Description ?? "",
        "createdAt" => ticket.CreatedAt.ToString("yyyy-MM-dd"),
        _ => ""
    };

    private static object? GetWorklogFieldValue(Domain.Entities.Worklog worklog, string field) => field switch
    {
        "date" => worklog.Date.ToString("yyyy-MM-dd"),
        "user" => worklog.User.DisplayName,
        "hours" => worklog.Hours,
        "description" => worklog.Description ?? "",
        "source" => worklog.Source.ToString(),
        "isBillable" => worklog.IsBillable ? "Yes" : "No",
        "invoiced" => worklog.Invoiced ?? "",
        _ => ""
    };
}
