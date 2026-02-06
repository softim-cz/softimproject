using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Interfaces;
using SoftimProject.Infrastructure;
using SoftimProject.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// Add infrastructure for DB access
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// MCP-compatible tool endpoints for AI assistants

// List projects
app.MapGet("/tools/projects", async (IApplicationDbContext db, CancellationToken ct) =>
{
    var projects = await db.Projects
        .Select(p => new { p.Id, p.Name, p.Code, Status = p.Status.ToString(), p.SpentHours, p.BudgetHours, p.HealthScore })
        .ToListAsync(ct);
    return Results.Ok(projects);
}).WithName("ListProjects").WithDescription("List all projects with their status and health");

// Get project tickets
app.MapGet("/tools/projects/{projectId:guid}/tickets", async (Guid projectId, IApplicationDbContext db, CancellationToken ct) =>
{
    var tickets = await db.Tickets
        .Where(t => t.ProjectId == projectId)
        .Select(t => new { t.Id, t.Title, Status = t.Status.ToString(), Priority = t.Priority.ToString(), Assignee = t.Assignee != null ? t.Assignee.DisplayName : null, t.DueDate })
        .ToListAsync(ct);
    return Results.Ok(tickets);
}).WithName("GetProjectTickets").WithDescription("Get all tickets for a specific project");

// Get ticket details
app.MapGet("/tools/tickets/{ticketId:guid}", async (Guid ticketId, IApplicationDbContext db, CancellationToken ct) =>
{
    var ticket = await db.Tickets
        .Where(t => t.Id == ticketId)
        .Select(t => new { t.Id, t.Title, t.Description, Status = t.Status.ToString(), Priority = t.Priority.ToString(), t.AiSummary, Assignee = t.Assignee != null ? t.Assignee.DisplayName : null })
        .FirstOrDefaultAsync(ct);
    return ticket is not null ? Results.Ok(ticket) : Results.NotFound();
}).WithName("GetTicketDetails").WithDescription("Get detailed information about a specific ticket");

// Log worklog
app.MapPost("/tools/worklogs", async (CreateWorklogRequest request, IApplicationDbContext db, CancellationToken ct) =>
{
    var worklog = new SoftimProject.Domain.Entities.Worklog
    {
        Id = Guid.NewGuid(),
        ProjectId = request.ProjectId,
        TicketId = request.TicketId,
        UserId = request.UserId,
        Date = DateOnly.Parse(request.Date),
        Hours = request.Hours,
        Description = request.Description,
        Source = SoftimProject.Domain.Enums.WorklogSource.Sync,
        IsBillable = true,
        CreatedAt = DateTime.UtcNow
    };
    db.Worklogs.Add(worklog);
    await db.SaveChangesAsync(ct);
    return Results.Created($"/tools/worklogs/{worklog.Id}", new { worklog.Id });
}).WithName("LogWorklog").WithDescription("Create a worklog entry for a project");

// Get worklogs summary
app.MapGet("/tools/projects/{projectId:guid}/worklogs", async (Guid projectId, string? from, string? to, IApplicationDbContext db, CancellationToken ct) =>
{
    var query = db.Worklogs.Where(w => w.ProjectId == projectId);

    if (DateOnly.TryParse(from, out var fromDate))
        query = query.Where(w => w.Date >= fromDate);
    if (DateOnly.TryParse(to, out var toDate))
        query = query.Where(w => w.Date <= toDate);

    var worklogs = await query
        .GroupBy(w => w.User.DisplayName)
        .Select(g => new { User = g.Key, TotalHours = g.Sum(w => w.Hours), Entries = g.Count() })
        .ToListAsync(ct);

    return Results.Ok(worklogs);
}).WithName("GetWorklogsSummary").WithDescription("Get worklog summary for a project, optionally filtered by date range");

app.Run();

record CreateWorklogRequest(Guid ProjectId, Guid? TicketId, Guid UserId, string Date, decimal Hours, string? Description);
