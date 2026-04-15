using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using SoftimProject.Application;
using SoftimProject.Application.Features.Worklogs.CreateWorklog;
using SoftimProject.Application.Interfaces;
using SoftimProject.Infrastructure.Persistence;
using SoftimProject.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddApplicationServices();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var currentUserService = context.RequestServices.GetRequiredService<ICurrentUserService>();
        await currentUserService.InitializeAsync(context.RequestAborted);
    }

    await next();
});

var tools = app.MapGroup("/tools").RequireAuthorization();

tools.MapGet("/projects", async (IApplicationDbContext db, CancellationToken ct) =>
{
    var projects = await db.Projects
        .AsNoTracking()
        .Select(p => new
        {
            p.Id,
            p.Name,
            p.Code,
            Status = p.Status.ToString(),
            p.SpentHours,
            p.BudgetHours,
            p.HealthScore
        })
        .ToListAsync(ct);

    return Results.Ok(projects);
}).WithName("ListProjects").WithDescription("List all projects with their status and health");

tools.MapGet("/projects/{projectId:guid}/tickets", async (Guid projectId, IApplicationDbContext db, ICurrentUserService currentUserService, CancellationToken ct) =>
{
    if (!await currentUserService.HasProjectAccessAsync(projectId, ct))
    {
        return Results.Forbid();
    }

    var tickets = await db.Tickets
        .AsNoTracking()
        .Where(t => t.ProjectId == projectId)
        .Select(t => new
        {
            t.Id,
            t.Title,
            Status = t.TaskState.Name,
            Priority = t.TicketPriority.Name,
            Assignee = t.Assignee != null ? t.Assignee.DisplayName : null,
            t.DueDate
        })
        .ToListAsync(ct);

    return Results.Ok(tickets);
}).WithName("GetProjectTickets").WithDescription("Get all tickets for a specific project");

tools.MapGet("/tickets/{ticketId:guid}", async (Guid ticketId, IApplicationDbContext db, ICurrentUserService currentUserService, CancellationToken ct) =>
{
    var ticket = await db.Tickets
        .AsNoTracking()
        .Where(t => t.Id == ticketId)
        .Select(t => new
        {
            t.Id,
            t.Title,
            t.Description,
            Status = t.TaskState.Name,
            Priority = t.TicketPriority.Name,
            t.AiSummary,
            Assignee = t.Assignee != null ? t.Assignee.DisplayName : null,
            t.ProjectId
        })
        .FirstOrDefaultAsync(ct);

    if (ticket is null)
    {
        return Results.NotFound();
    }

    if (!await currentUserService.HasProjectAccessAsync(ticket.ProjectId, ct))
    {
        return Results.Forbid();
    }

    return Results.Ok(ticket);
}).WithName("GetTicketDetails").WithDescription("Get detailed information about a specific ticket");

tools.MapPost("/worklogs", async (CreateWorklogRequest request, IMediator mediator, ICurrentUserService currentUserService, CancellationToken ct) =>
{
    if (!await currentUserService.HasProjectAccessAsync(request.ProjectId, ct))
    {
        return Results.Forbid();
    }

    var id = await mediator.Send(
        new CreateWorklogCommand(
            request.ProjectId,
            request.TicketId,
            DateOnly.Parse(request.Date),
            request.Hours,
            request.Description,
            request.IsBillable),
        ct);

    return Results.Created($"/tools/worklogs/{id}", new { Id = id });
}).WithName("LogWorklog").WithDescription("Create a worklog entry for a project");

tools.MapGet("/projects/{projectId:guid}/worklogs", async (Guid projectId, string? from, string? to, IApplicationDbContext db, ICurrentUserService currentUserService, CancellationToken ct) =>
{
    if (!await currentUserService.HasProjectAccessAsync(projectId, ct))
    {
        return Results.Forbid();
    }

    var query = db.Worklogs.AsNoTracking().Where(w => w.ProjectId == projectId);

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

record CreateWorklogRequest(Guid ProjectId, Guid? TicketId, string Date, decimal Hours, string? Description, bool IsBillable);


