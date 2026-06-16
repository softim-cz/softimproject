using System.Globalization;
using System.Text;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Worklogs.ImportWorklogs;

/// <summary>
/// Bulk-imports worklogs from an .xlsx or .csv file (#53). Mandatory columns: ticket id,
/// date, hours, description. Duplicates are detected on (user, ticket, date, hours,
/// description) — the same person can legitimately log a ticket twice on one day with
/// different hours/notes — and are skipped with a warning rather than silently dropped.
/// </summary>
public sealed record ImportWorklogsCommand(
    Guid ProjectId,
    string FileName,
    byte[] Content,
    Guid? OverrideUserId = null) : IRequest<ImportWorklogsResult>, IRequireProjectRole
{
    public ProjectRole RequiredProjectRole => ProjectRole.Developer;
}

public sealed record ImportWorklogsResult(
    int TotalRows,
    int Created,
    int Duplicates,
    int Errors,
    IReadOnlyList<ImportWorklogIssue> Issues);

/// <summary>A skipped row: Type is "Duplicate" or "Error", Row is the 1-based file row.</summary>
public sealed record ImportWorklogIssue(int Row, string Type, string Message);

public sealed class ImportWorklogsCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService) : IRequestHandler<ImportWorklogsCommand, ImportWorklogsResult>
{
    private static readonly string[] DateFormats =
    {
        "yyyy-MM-dd", "d.M.yyyy", "dd.MM.yyyy", "yyyy/M/d", "M/d/yyyy", "d/M/yyyy"
    };

    public async Task<ImportWorklogsResult> Handle(ImportWorklogsCommand request, CancellationToken cancellationToken)
    {
        var callerId = currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Current user is not initialized.");

        var ownerId = callerId;
        if (request.OverrideUserId.HasValue && request.OverrideUserId.Value != callerId)
        {
            if (!currentUserService.IsInRole("Admin"))
                throw new UnauthorizedAccessException("Only Admin can import worklogs on behalf of another user.");

            var exists = await dbContext.Users.AnyAsync(u => u.Id == request.OverrideUserId.Value, cancellationToken);
            if (!exists)
                throw new NotFoundException(nameof(User), request.OverrideUserId.Value);

            ownerId = request.OverrideUserId.Value;
        }

        var (headers, rows) = Parse(request.FileName, request.Content);

        var ticketCol = FindColumn(headers, "ticket", "id ticketu", "ticket id", "ticketkey", "klíč", "klic");
        var dateCol = FindColumn(headers, "date", "datum");
        var hoursCol = FindColumn(headers, "hours", "počet hodin", "pocet hodin", "hodiny");
        var descCol = FindColumn(headers, "description", "popis");

        var missing = new List<string>();
        if (ticketCol < 0) missing.Add("ticket");
        if (dateCol < 0) missing.Add("date");
        if (hoursCol < 0) missing.Add("hours");
        if (descCol < 0) missing.Add("description");
        if (missing.Count > 0)
            throw new ValidationException($"Missing required column(s): {string.Join(", ", missing)}.");

        // Resolve tickets within the project up front (key parsing tolerates "CODE-123" or "123").
        var ticketsByNumber = await dbContext.Tickets
            .Where(t => t.ProjectId == request.ProjectId)
            .Select(t => new { t.Id, t.Number })
            .ToDictionaryAsync(t => t.Number, t => t.Id, cancellationToken);

        // Existing worklogs of this owner in the project form the duplicate baseline.
        var existing = await dbContext.Worklogs
            .Where(w => w.UserId == ownerId && w.Ticket.ProjectId == request.ProjectId)
            .Select(w => new { w.TicketId, w.Date, w.Hours, w.Description })
            .ToListAsync(cancellationToken);
        var seen = new HashSet<string>(existing.Select(w => DuplicateKey(w.TicketId, w.Date, w.Hours, w.Description)));

        var issues = new List<ImportWorklogIssue>();
        var affectedTickets = new HashSet<Guid>();
        var now = DateTime.UtcNow;
        var created = 0;
        var duplicates = 0;
        var dataRows = 0;

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var fileRow = i + 2; // header is row 1

            // Skip fully blank rows without counting them.
            if (IsBlank(Cell(row, ticketCol)) && IsBlank(Cell(row, dateCol))
                && IsBlank(Cell(row, hoursCol)) && IsBlank(Cell(row, descCol)))
                continue;

            dataRows++;

            var ticketId = ResolveTicket(Cell(row, ticketCol), ticketsByNumber);
            if (ticketId is null)
            {
                issues.Add(new ImportWorklogIssue(fileRow, "Error", $"Unknown ticket '{AsText(Cell(row, ticketCol))}'."));
                continue;
            }

            var date = ToDateOnly(Cell(row, dateCol));
            if (date is null)
            {
                issues.Add(new ImportWorklogIssue(fileRow, "Error", $"Invalid date '{AsText(Cell(row, dateCol))}'."));
                continue;
            }

            var hours = ToDecimal(Cell(row, hoursCol));
            if (hours is null || hours <= 0 || hours > 24)
            {
                issues.Add(new ImportWorklogIssue(fileRow, "Error", $"Invalid hours '{AsText(Cell(row, hoursCol))}'."));
                continue;
            }

            var description = AsText(Cell(row, descCol)).Trim();
            if (description.Length == 0)
            {
                issues.Add(new ImportWorklogIssue(fileRow, "Error", "Description is required."));
                continue;
            }
            if (description.Length > 2000)
            {
                issues.Add(new ImportWorklogIssue(fileRow, "Error", "Description exceeds 2000 characters."));
                continue;
            }

            var key = DuplicateKey(ticketId.Value, date.Value, hours.Value, description);
            if (!seen.Add(key))
            {
                duplicates++;
                issues.Add(new ImportWorklogIssue(fileRow, "Duplicate", "A matching worklog already exists; skipped."));
                continue;
            }

            dbContext.Worklogs.Add(new Worklog
            {
                Id = Guid.NewGuid(),
                TicketId = ticketId.Value,
                UserId = ownerId,
                Date = date.Value,
                Hours = hours.Value,
                Description = description,
                Source = WorklogSource.Import,
                IsBillable = true,
                CreatedAt = now
            });
            affectedTickets.Add(ticketId.Value);
            created++;
        }

        if (created > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            foreach (var ticketId in affectedTickets)
                await CumulativeWorkedHoursCalculator.RecalculateUpwardAsync(dbContext, ticketId, cancellationToken);
        }

        return new ImportWorklogsResult(dataRows, created, duplicates, issues.Count(x => x.Type == "Error"), issues);
    }

    private static string DuplicateKey(Guid ticketId, DateOnly date, decimal hours, string? description) =>
        $"{ticketId}|{date:yyyy-MM-dd}|{hours.ToString(CultureInfo.InvariantCulture)}|{description?.Trim().ToLowerInvariant()}";

    private static Guid? ResolveTicket(object? raw, IReadOnlyDictionary<int, Guid> ticketsByNumber)
    {
        var text = AsText(raw).Trim();
        if (text.Length == 0) return null;

        // Accept "CODE-123" (use the trailing number) or a bare "123".
        var dash = text.LastIndexOf('-');
        var numberPart = dash >= 0 ? text[(dash + 1)..] : text;
        if (int.TryParse(numberPart.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
            && ticketsByNumber.TryGetValue(number, out var id))
            return id;

        return null;
    }

    private static DateOnly? ToDateOnly(object? raw)
    {
        switch (raw)
        {
            case DateTime dt:
                return DateOnly.FromDateTime(dt);
            case double d:
                return DateOnly.FromDateTime(DateTime.FromOADate(d));
        }

        var text = AsText(raw).Trim();
        if (text.Length == 0) return null;

        if (DateTime.TryParseExact(text, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact))
            return DateOnly.FromDateTime(exact);
        if (DateTime.TryParse(text, CultureInfo.GetCultureInfo("cs-CZ"), DateTimeStyles.None, out var cz))
            return DateOnly.FromDateTime(cz);
        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var inv))
            return DateOnly.FromDateTime(inv);

        return null;
    }

    private static decimal? ToDecimal(object? raw)
    {
        switch (raw)
        {
            case double d:
                return (decimal)d;
            case decimal m:
                return m;
            case int i:
                return i;
            case long l:
                return l;
        }

        var text = AsText(raw).Trim().Replace(',', '.');
        if (text.Length == 0) return null;
        return decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var value) ? value : null;
    }

    private static object? Cell(object?[] row, int index) => index >= 0 && index < row.Length ? row[index] : null;

    private static bool IsBlank(object? raw) => AsText(raw).Trim().Length == 0;

    private static string AsText(object? raw) => raw switch
    {
        null => string.Empty,
        DateTime dt => dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => raw.ToString() ?? string.Empty
    };

    private static int FindColumn(IReadOnlyList<string> headers, params string[] names)
    {
        for (var i = 0; i < headers.Count; i++)
            if (names.Any(n => string.Equals(headers[i], n, StringComparison.OrdinalIgnoreCase)))
                return i;
        return -1;
    }

    private static (List<string> Headers, List<object?[]> Rows) Parse(string fileName, byte[] content)
    {
        var isCsv = fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase);
        return isCsv ? ParseCsv(content) : ParseXlsx(content);
    }

    private static (List<string>, List<object?[]>) ParseXlsx(byte[] content)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var ms = new MemoryStream(content);
        using var package = new ExcelPackage(ms);
        var ws = package.Workbook.Worksheets.FirstOrDefault()
            ?? throw new ValidationException("The spreadsheet contains no worksheets.");

        var dim = ws.Dimension;
        if (dim is null) return (new List<string>(), new List<object?[]>());

        var colCount = dim.End.Column;
        var headers = new List<string>(colCount);
        for (var c = 1; c <= colCount; c++)
            headers.Add((ws.Cells[dim.Start.Row, c].Text ?? string.Empty).Trim());

        var rows = new List<object?[]>();
        for (var r = dim.Start.Row + 1; r <= dim.End.Row; r++)
        {
            var arr = new object?[colCount];
            for (var c = 1; c <= colCount; c++)
                arr[c - 1] = ws.Cells[r, c].Value;
            rows.Add(arr);
        }

        return (headers, rows);
    }

    private static (List<string>, List<object?[]>) ParseCsv(byte[] content)
    {
        var text = Encoding.UTF8.GetString(content).TrimStart('﻿');
        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        if (lines.Length == 0 || string.IsNullOrWhiteSpace(lines[0]))
            return (new List<string>(), new List<object?[]>());

        // Czech Excel exports default to ';'; fall back to ','.
        var delimiter = lines[0].Contains(';') ? ';' : ',';
        var headers = ParseCsvLine(lines[0], delimiter).Select(h => h.Trim()).ToList();

        var rows = new List<object?[]>();
        for (var i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            var cells = ParseCsvLine(lines[i], delimiter);
            var arr = new object?[headers.Count];
            for (var c = 0; c < headers.Count; c++)
                arr[c] = c < cells.Count ? cells[c] : null;
            rows.Add(arr);
        }

        return (headers, rows);
    }

    private static List<string> ParseCsvLine(string line, char delimiter)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                    else inQuotes = false;
                }
                else sb.Append(ch);
            }
            else if (ch == '"') inQuotes = true;
            else if (ch == delimiter) { result.Add(sb.ToString()); sb.Clear(); }
            else sb.Append(ch);
        }
        result.Add(sb.ToString());
        return result;
    }
}
