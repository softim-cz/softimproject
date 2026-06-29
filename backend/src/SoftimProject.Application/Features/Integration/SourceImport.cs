using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoftimProject.Application.Integrations;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Integration;

// Provider-agnostic import/connection flow (#144 unification): one path for EasyProject,
// Jira and Redmine, driven by the chosen SourceSystem and ISourceConnector. External ids are
// canonical strings throughout.

public sealed record SourceConnectionInput(SyncType SourceSystem, string BaseUrl, string ApiToken);

public sealed record ConnectionTestResult(bool Success, string? Error);

public sealed record SourceProjectPreviewDto(string ExternalId, string Name, bool AlreadyImported);

public sealed record SourceLookupDto(string ExternalId, string Name, bool IsClosed, Guid? SuggestedId, string? SuggestedName);

public sealed record SourceLookupsResult(
    List<SourceLookupDto> Types,
    List<SourceLookupDto> Statuses,
    List<SourceLookupDto> Priorities);

public sealed record SourceUserMappingDto(string ExternalId, string Name, string? Email, Guid? MatchedUserId, string? MatchedUserName);

internal static class SourceConnectorResolver
{
    public static ISourceConnector Resolve(IEnumerable<ISourceConnector> connectors, SyncType system) =>
        connectors.FirstOrDefault(c => c.SourceSystem == system)
            ?? throw new InvalidOperationException($"No connector registered for {system}.");
}

// --- Test connection ---

public sealed record TestSourceConnectionQuery(SourceConnectionInput Input) : IRequest<ConnectionTestResult>;

public sealed class TestSourceConnectionQueryHandler(IEnumerable<ISourceConnector> connectors)
    : IRequestHandler<TestSourceConnectionQuery, ConnectionTestResult>
{
    public async Task<ConnectionTestResult> Handle(TestSourceConnectionQuery request, CancellationToken cancellationToken)
    {
        var connector = SourceConnectorResolver.Resolve(connectors, request.Input.SourceSystem);
        var ctx = new SourceConnectionContext(request.Input.BaseUrl, request.Input.ApiToken);
        var (success, error) = await connector.TestConnectionAsync(ctx, cancellationToken);
        return new ConnectionTestResult(success, error);
    }
}

// --- Fetch projects ---

public sealed record FetchSourceProjectsQuery(SourceConnectionInput Input) : IRequest<List<SourceProjectPreviewDto>>;

public sealed class FetchSourceProjectsQueryHandler(
    IEnumerable<ISourceConnector> connectors,
    IApplicationDbContext dbContext) : IRequestHandler<FetchSourceProjectsQuery, List<SourceProjectPreviewDto>>
{
    public async Task<List<SourceProjectPreviewDto>> Handle(FetchSourceProjectsQuery request, CancellationToken cancellationToken)
    {
        var connector = SourceConnectorResolver.Resolve(connectors, request.Input.SourceSystem);
        var ctx = new SourceConnectionContext(request.Input.BaseUrl, request.Input.ApiToken);
        var projects = await connector.GetProjectsAsync(ctx, cancellationToken);

        var systemName = request.Input.SourceSystem.ToString();
        var imported = (await dbContext.Projects
            .Where(p => p.ExternalSystem == systemName && p.ExternalProjectId != null)
            .Select(p => p.ExternalProjectId!)
            .ToListAsync(cancellationToken)).ToHashSet();

        return projects
            .Select(p => new SourceProjectPreviewDto(p.ExternalId, p.Name, imported.Contains(p.ExternalId)))
            .ToList();
    }
}

// --- Fetch lookups (with suggested mapping to ProjectMan lookups by name/keyword) ---

public sealed record FetchSourceLookupsQuery(SourceConnectionInput Input) : IRequest<SourceLookupsResult>;

public sealed class FetchSourceLookupsQueryHandler(
    IEnumerable<ISourceConnector> connectors,
    IApplicationDbContext dbContext) : IRequestHandler<FetchSourceLookupsQuery, SourceLookupsResult>
{
    public async Task<SourceLookupsResult> Handle(FetchSourceLookupsQuery request, CancellationToken cancellationToken)
    {
        var connector = SourceConnectorResolver.Resolve(connectors, request.Input.SourceSystem);
        var ctx = new SourceConnectionContext(request.Input.BaseUrl, request.Input.ApiToken);
        var lookups = await connector.GetLookupsAsync(ctx, cancellationToken);

        var taskTypes = await dbContext.TaskTypes.Where(t => t.IsActive).ToListAsync(cancellationToken);
        var taskStates = await dbContext.TaskStates.Where(ts => ts.IsActive).ToListAsync(cancellationToken);
        var priorities = await dbContext.TicketPriorities.Where(tp => tp.IsActive).ToListAsync(cancellationToken);

        var types = lookups.Types.Select(t =>
        {
            var match = taskTypes.FirstOrDefault(tt => tt.Name.Equals(t.Name, StringComparison.OrdinalIgnoreCase));
            return new SourceLookupDto(t.ExternalId, t.Name, false, match?.Id, match?.Name);
        }).ToList();

        var statuses = lookups.Statuses.Select(s =>
        {
            var match = LookupMatching.MapStatus(s.Name, s.IsClosed, taskStates);
            return new SourceLookupDto(s.ExternalId, s.Name, s.IsClosed, match?.Id, match?.Name);
        }).ToList();

        var prios = lookups.Priorities.Select(p =>
        {
            var match = LookupMatching.MapPriority(p.Name, priorities);
            return new SourceLookupDto(p.ExternalId, p.Name, false, match?.Id, match?.Name);
        }).ToList();

        return new SourceLookupsResult(types, statuses, prios);
    }
}

// --- Fetch users (suggested match by email) ---

public sealed record FetchSourceUsersQuery(SourceConnectionInput Input) : IRequest<List<SourceUserMappingDto>>;

public sealed class FetchSourceUsersQueryHandler(
    IEnumerable<ISourceConnector> connectors,
    IApplicationDbContext dbContext) : IRequestHandler<FetchSourceUsersQuery, List<SourceUserMappingDto>>
{
    public async Task<List<SourceUserMappingDto>> Handle(FetchSourceUsersQuery request, CancellationToken cancellationToken)
    {
        var connector = SourceConnectorResolver.Resolve(connectors, request.Input.SourceSystem);
        var ctx = new SourceConnectionContext(request.Input.BaseUrl, request.Input.ApiToken);
        var users = await connector.GetUsersAsync(ctx, cancellationToken);
        var spUsers = await dbContext.Users.Where(u => u.IsActive).ToListAsync(cancellationToken);

        return users.Select(u =>
        {
            var name = string.Join(" ", new[] { u.FirstName, u.LastName }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
            if (string.IsNullOrEmpty(name)) name = u.Login ?? u.ExternalId;
            var mail = u.Email?.ToLowerInvariant();
            var match = mail != null ? spUsers.FirstOrDefault(su => su.Email.ToLowerInvariant() == mail) : null;
            return new SourceUserMappingDto(u.ExternalId, name, u.Email, match?.Id, match?.DisplayName);
        }).ToList();
    }
}

// Provider-agnostic name/keyword matching (same heuristics as the original EasyProject wizard).
internal static class LookupMatching
{
    public static TaskState? MapStatus(string name, bool isClosed, List<TaskState> taskStates)
    {
        if (isClosed) return taskStates.FirstOrDefault(ts => ts.IsClosedState);
        var exact = taskStates.FirstOrDefault(ts => ts.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (exact != null) return exact;
        var lower = name.ToLowerInvariant();
        if (lower.Contains("new") || lower.Contains("backlog"))
            return taskStates.FirstOrDefault(ts => ts.Name.Contains("Backlog", StringComparison.OrdinalIgnoreCase)) ?? taskStates.FirstOrDefault(ts => ts.IsDefault);
        if (lower.Contains("progress") || lower.Contains("working"))
            return taskStates.FirstOrDefault(ts => ts.Name.Contains("Progress", StringComparison.OrdinalIgnoreCase));
        if (lower.Contains("review") || lower.Contains("feedback"))
            return taskStates.FirstOrDefault(ts => ts.Name.Contains("Review", StringComparison.OrdinalIgnoreCase));
        if (lower.Contains("done") || lower.Contains("resolved"))
            return taskStates.FirstOrDefault(ts => ts.Name.Contains("Done", StringComparison.OrdinalIgnoreCase));
        if (lower.Contains("todo") || lower.Contains("ready") || lower.Contains("assigned"))
            return taskStates.FirstOrDefault(ts => ts.Name.Contains("Todo", StringComparison.OrdinalIgnoreCase) || ts.Name.Contains("To Do", StringComparison.OrdinalIgnoreCase));
        return taskStates.FirstOrDefault(ts => ts.IsDefault);
    }

    public static TicketPriority? MapPriority(string name, List<TicketPriority> priorities)
    {
        var exact = priorities.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (exact != null) return exact;
        var lower = name.ToLowerInvariant();
        if (lower.Contains("critical") || lower.Contains("urgent") || lower.Contains("immediate"))
            return priorities.FirstOrDefault(p => p.Name.Contains("Critical", StringComparison.OrdinalIgnoreCase));
        if (lower.Contains("high")) return priorities.FirstOrDefault(p => p.Name.Contains("High", StringComparison.OrdinalIgnoreCase));
        if (lower.Contains("low")) return priorities.FirstOrDefault(p => p.Name.Contains("Low", StringComparison.OrdinalIgnoreCase));
        return priorities.FirstOrDefault(p => p.Name.Contains("Medium", StringComparison.OrdinalIgnoreCase)) ?? priorities.FirstOrDefault(p => p.IsDefault);
    }
}

// --- Remember connection (persist after a successful test, before any import) ---

public sealed record RememberSourceConnectionCommand(SourceConnectionInput Input) : IRequest<Guid>, IRequireRole
{
    public string RequiredRole => "Admin";
}

public sealed class RememberSourceConnectionCommandHandler(
    ICurrentUserService currentUserService,
    IIntegrationConnectionWriter connectionWriter)
    : IRequestHandler<RememberSourceConnectionCommand, Guid>
{
    public Task<Guid> Handle(RememberSourceConnectionCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedAccessException("User not authenticated.");
        return connectionWriter.RememberConnectionAsync(
            request.Input.SourceSystem, request.Input.BaseUrl, request.Input.ApiToken, userId, cancellationToken);
    }
}

// --- Start import (creates job + connection, runs the engine in the background) ---

public sealed record StartSourceImportCommand(
    SyncType SourceSystem,
    string BaseUrl,
    string ApiKey,
    List<string> ProjectExternalIds,
    Guid TargetProjectTemplateId,
    Dictionary<string, Guid?> TrackerMapping,
    Dictionary<string, Guid> StatusMapping,
    Dictionary<string, Guid> PriorityMapping,
    Dictionary<string, Guid?> UserMapping,
    bool SkipClosedIssues,
    bool SkipAttachments,
    bool ImportComments,
    bool ImportWorklogs,
    bool ImportChecklists,
    bool CreateMissingUsers,
    Dictionary<string, string>? AutoCreateTrackers = null,
    Dictionary<string, string>? AutoCreateStatuses = null,
    Dictionary<string, bool>? AutoCreateStatusIsClosed = null,
    Dictionary<string, string>? AutoCreatePriorities = null,
    Guid? TargetCompanyId = null,
    bool EnableIncrementalSync = false,
    int SyncIntervalMinutes = 1440) : IRequest<Guid>;

public sealed class StartSourceImportCommandValidator : AbstractValidator<StartSourceImportCommand>
{
    public StartSourceImportCommandValidator(IApplicationDbContext dbContext)
    {
        RuleFor(x => x.TargetProjectTemplateId).NotEmpty().WithMessage("Cílová šablona projektu je povinná.");
        RuleFor(x => x.TargetProjectTemplateId)
            .MustAsync(async (id, ct) => await dbContext.ProjectTemplates.AnyAsync(t => t.Id == id && t.IsActive, ct))
            .WithMessage("Cílová šablona projektu neexistuje nebo není aktivní.");
        RuleFor(x => x.SyncIntervalMinutes).GreaterThanOrEqualTo(60).When(x => x.EnableIncrementalSync)
            .WithMessage("Interval synchronizace musí být alespoň 60 minut.");
    }
}

public sealed class StartSourceImportCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IMigrationProgressTracker progressTracker,
    IMigrationNotifier notifier,
    IIntegrationConnectionWriter connectionWriter,
    IServiceScopeFactory scopeFactory) : IRequestHandler<StartSourceImportCommand, Guid>
{
    public async Task<Guid> Handle(StartSourceImportCommand request, CancellationToken cancellationToken)
    {
        var jobId = Guid.NewGuid();
        var userId = currentUserService.UserId ?? throw new UnauthorizedAccessException("User not authenticated.");

        dbContext.MigrationJobs.Add(new MigrationJob
        {
            Id = jobId,
            InitiatedByUserId = userId,
            SourceSystem = request.SourceSystem.ToString(),
            SourceBaseUrl = request.BaseUrl,
            Status = MigrationStatus.Pending,
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        var connectionId = await connectionWriter.UpsertForImportAsync(request, userId, cancellationToken);
        progressTracker.Init(jobId);

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<ISourceImportService>();
                await svc.ExecuteAsync(jobId, request, connectionId);
            }
            catch (Exception ex)
            {
                progressTracker.Fail(jobId, ex.Message);
                var progress = progressTracker.GetProgress(jobId);
                if (progress != null) await notifier.NotifyProgressAsync(jobId, progress);
                try
                {
                    using var dbScope = scopeFactory.CreateScope();
                    var db = dbScope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                    var failed = await db.MigrationJobs.FindAsync([jobId], CancellationToken.None);
                    if (failed != null)
                    {
                        failed.Status = MigrationStatus.Failed;
                        failed.CompletedAt = DateTime.UtcNow;
                        failed.ErrorLog = System.Text.Json.JsonSerializer.Serialize(new[] { ex.Message });
                        await db.SaveChangesAsync(CancellationToken.None);
                    }
                }
                catch { /* best-effort */ }
            }
        }, cancellationToken);

        return jobId;
    }
}
