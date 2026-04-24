using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using SoftimProject.Application;
using SoftimProject.Application.Features.Projects.GetProjects;
using SoftimProject.Application.Features.Tickets.GetTicketById;
using SoftimProject.Application.Features.Tickets.GetTickets;
using SoftimProject.Application.Features.Worklogs.CreateWorklog;
using SoftimProject.Application.Features.Worklogs.GetWorklogs;
using SoftimProject.Application.Interfaces;
using SoftimProject.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
// #18: share the full Application + Infrastructure stack with WebApi. Tools used
// to register DbContext and ICurrentUserService inline; now Infrastructure does it,
// which keeps behaviour (background services, Polly pipelines, audit recorders) in
// sync with what the WebApi process exposes.
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddHttpContextAccessor();

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

// Every tool below delegates to the same MediatR handler WebApi uses. That
// unlocks two things for free:
//   1. Authorization — IRequireProjectAccess, IRequireProjectRole etc. fire via
//      `AuthorizationBehavior`, so a forbidden call becomes a 403 without inline checks.
//   2. Shape stability — tools return the same DTOs the REST clients already consume,
//      so LLM-driven tooling doesn't have to speak a second schema.

tools.MapGet("/projects", async (IMediator mediator, int page, int pageSize, CancellationToken ct) =>
{
    var pagedResult = await mediator.Send(
        new GetProjectsQuery(
            page <= 0 ? 1 : page,
            pageSize <= 0 ? 50 : pageSize),
        ct);
    return Results.Ok(pagedResult);
}).WithName("ListProjects")
  .WithDescription("List projects the caller can see (paged).");

tools.MapGet("/projects/{projectId:guid}/tickets", async (
    Guid projectId,
    string? search,
    int page,
    int pageSize,
    IMediator mediator,
    CancellationToken ct) =>
{
    var pagedResult = await mediator.Send(
        new GetTicketsQuery(
            projectId,
            SearchTerm: search,
            Page: page <= 0 ? 1 : page,
            PageSize: pageSize <= 0 ? 25 : pageSize),
        ct);
    return Results.Ok(pagedResult);
}).WithName("GetProjectTickets")
  .WithDescription("Page through tickets for a project. IRequireProjectAccess is enforced; callers without membership get 403.");

// Breaking change vs. the old /tools/tickets/{ticketId} route: the query's
// authorization guard needs a projectId, and forcing it into the URL closes the
// previous hole where a caller without project membership could fetch any ticket
// by guessing the ticket id. The old route has been removed intentionally.
tools.MapGet("/projects/{projectId:guid}/tickets/{ticketId:guid}", async (
    Guid projectId,
    Guid ticketId,
    IMediator mediator,
    CancellationToken ct) =>
{
    var ticket = await mediator.Send(new GetTicketByIdQuery(projectId, ticketId), ct);
    return Results.Ok(ticket);
}).WithName("GetTicketDetails")
  .WithDescription("Get the full detail of a single ticket (scoped to its project).");

tools.MapPost("/worklogs", async (CreateWorklogRequest request, IMediator mediator, CancellationToken ct) =>
{
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
}).WithName("LogWorklog")
  .WithDescription("Create a worklog entry. IRequireProjectRole(Developer) is enforced; Guest role returns 403.");

tools.MapGet("/projects/{projectId:guid}/worklogs", async (
    Guid projectId,
    string? from,
    string? to,
    int page,
    int pageSize,
    IMediator mediator,
    CancellationToken ct) =>
{
    DateOnly? fromDate = DateOnly.TryParse(from, out var f) ? f : null;
    DateOnly? toDate = DateOnly.TryParse(to, out var t) ? t : null;
    var pagedResult = await mediator.Send(
        new GetWorklogsQuery(
            ProjectId: projectId,
            From: fromDate,
            To: toDate,
            Page: page <= 0 ? 1 : page,
            PageSize: pageSize <= 0 ? 50 : pageSize),
        ct);
    return Results.Ok(pagedResult);
}).WithName("GetWorklogs")
  .WithDescription("Page through project worklogs within an optional date window.");

app.Run();

// Exposed for WebApplicationFactory<Program> in integration tests.
public partial class Program { }

public sealed record CreateWorklogRequest(
    Guid ProjectId,
    Guid? TicketId,
    string Date,
    decimal Hours,
    string? Description,
    bool IsBillable);
