using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Migration.EasyProject;

// Pre-flight check run from the admin UI before kicking off an import. Talks to
// EasyProject and to our own DB; everything surfaces as a list of issues
// (Blocking stops the Start button, Warning is advisory).
public sealed record ValidateMigrationQuery(
    string BaseUrl,
    string ApiKey,
    List<int> ProjectIds) : IRequest<MigrationValidationResult>, IRequireRole
{
    public string RequiredRole => "Admin";
}

public sealed record MigrationValidationIssue(string Severity, string Message);

public sealed record MigrationValidationResult(
    bool CredentialsValid,
    string? ConnectedAs,
    int EpProjectCount,
    IReadOnlyList<MigrationProjectPreview> SelectedProjects,
    IReadOnlyList<MigrationValidationIssue> Issues);

public sealed record MigrationProjectPreview(
    int EpProjectId,
    string Name,
    bool AlreadyMigrated,
    Guid? SpProjectId);

public sealed class ValidateMigrationQueryHandler(
    IApplicationDbContext dbContext,
    IEasyProjectApiClient apiClient) : IRequestHandler<ValidateMigrationQuery, MigrationValidationResult>
{
    public async Task<MigrationValidationResult> Handle(
        ValidateMigrationQuery request,
        CancellationToken cancellationToken)
    {
        var issues = new List<MigrationValidationIssue>();

        // 1) Credentials — TestConnectionAsync hits a cheap EP endpoint and bubbles the error.
        var (connectionOk, connectionError) = await apiClient.TestConnectionAsync(
            request.BaseUrl, request.ApiKey, cancellationToken);
        if (!connectionOk)
        {
            issues.Add(new MigrationValidationIssue("Blocking",
                $"EasyProject connection failed: {connectionError ?? "unknown error"}"));
            // Without creds we can't validate anything else. Return early.
            return new MigrationValidationResult(false, null, 0, Array.Empty<MigrationProjectPreview>(), issues);
        }

        // With creds good, the project list query doubles as the "who am I" — we
        // don't surface a concrete user name because TestConnectionAsync doesn't carry it.
        string? connectedAs = request.BaseUrl;

        // 2) Source project list — confirm the requested ids exist.
        var epProjects = await apiClient.GetProjectsAsync(request.BaseUrl, request.ApiKey, cancellationToken);
        var byId = epProjects.ToDictionary(p => p.Id);

        if (request.ProjectIds.Count == 0)
        {
            issues.Add(new MigrationValidationIssue("Warning",
                "No projects selected — Start will be a no-op."));
        }

        var missingIds = request.ProjectIds.Where(id => !byId.ContainsKey(id)).ToList();
        foreach (var missing in missingIds)
        {
            issues.Add(new MigrationValidationIssue("Blocking",
                $"Project id {missing} not visible with the given API key."));
        }

        // 3) Collision detection against our DB — a SP project already linked to an EP
        // project with the same ExternalProjectId will be upserted, which is intended;
        // we just flag it so the admin knows to expect updates, not inserts.
        var externalIds = request.ProjectIds.Select(id => id.ToString()).ToList();
        var alreadyMigrated = await dbContext.Projects
            .Where(p => p.ExternalSystem == "EasyProject" && externalIds.Contains(p.ExternalProjectId!))
            .Select(p => new { p.ExternalProjectId, p.Id })
            .ToListAsync(cancellationToken);
        var migratedLookup = alreadyMigrated.ToDictionary(p => p.ExternalProjectId!, p => p.Id);

        var selected = request.ProjectIds
            .Where(id => byId.ContainsKey(id))
            .Select(id => new MigrationProjectPreview(
                id,
                byId[id].Name,
                AlreadyMigrated: migratedLookup.ContainsKey(id.ToString()),
                SpProjectId: migratedLookup.TryGetValue(id.ToString(), out var existing) ? existing : null))
            .ToList();

        var reimports = selected.Count(p => p.AlreadyMigrated);
        if (reimports > 0)
        {
            issues.Add(new MigrationValidationIssue("Warning",
                $"{reimports} project(s) already migrated — upserts will be applied. Running twice is safe; existing tickets/comments/attachments stay deduplicated via ExternalId."));
        }

        return new MigrationValidationResult(true, connectedAs, epProjects.Count, selected, issues);
    }
}
