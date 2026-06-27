using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Migration.EasyProject;

// TargetProjectTemplateId — povinné. Šablona, do které spadnou importované
// TaskStates / TicketPriorities z EP (auto-create) i importované projekty
// (Project.ProjectTemplateId). Tím se odstraní orphan lookup rows bez šablony
// a křížení názvů mezi šablonami.
public sealed record StartMigrationCommand(
    string BaseUrl,
    string ApiKey,
    List<int> ProjectIds,
    Guid TargetProjectTemplateId,
    Dictionary<int, Guid?> TrackerMapping,
    Dictionary<int, Guid> StatusMapping,
    Dictionary<int, Guid> PriorityMapping,
    Dictionary<int, Guid?> UserMapping,
    bool SkipClosedIssues,
    bool SkipAttachments,
    bool ImportComments,
    bool ImportWorklogs,
    bool ImportChecklists,
    bool CreateMissingUsers,
    Dictionary<int, string>? AutoCreateTrackers = null,
    Dictionary<int, string>? AutoCreateStatuses = null,
    Dictionary<int, bool>? AutoCreateStatusIsClosed = null,
    Dictionary<int, string>? AutoCreatePriorities = null) : IRequest<Guid>;

public sealed class StartMigrationCommandValidator : AbstractValidator<StartMigrationCommand>
{
    public StartMigrationCommandValidator(IApplicationDbContext dbContext)
    {
        RuleFor(x => x.TargetProjectTemplateId)
            .NotEmpty()
            .WithMessage("Cílová šablona projektu je povinná.");

        RuleFor(x => x.TargetProjectTemplateId)
            .MustAsync(async (id, ct) =>
                await dbContext.ProjectTemplates.AnyAsync(t => t.Id == id && t.IsActive, ct))
            .WithMessage("Cílová šablona projektu neexistuje nebo není aktivní.");
    }
}

public sealed class StartMigrationCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IMigrationProgressTracker progressTracker,
    IMigrationNotifier notifier,
    IIntegrationConnectionWriter connectionWriter,
    IServiceScopeFactory scopeFactory) : IRequestHandler<StartMigrationCommand, Guid>
{
    public async Task<Guid> Handle(StartMigrationCommand request, CancellationToken cancellationToken)
    {
        var jobId = Guid.NewGuid();
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        var job = new MigrationJob
        {
            Id = jobId,
            InitiatedByUserId = userId,
            SourceSystem = "EasyProject",
            SourceBaseUrl = request.BaseUrl,
            Status = MigrationStatus.Pending,
            StartedAt = DateTime.UtcNow,
            // Full StartMigrationCommand minus ApiKey — the stored config is what
            // ResumeMigrationCommand hydrates back into a command. Api key stays out
            // on purpose (it's a rotatable secret; callers re-supply it on resume).
            Configuration = System.Text.Json.JsonSerializer.Serialize(new StoredMigrationConfig(
                request.BaseUrl,
                request.ProjectIds,
                request.TargetProjectTemplateId,
                request.TrackerMapping,
                request.StatusMapping,
                request.PriorityMapping,
                request.UserMapping,
                request.SkipClosedIssues,
                request.SkipAttachments,
                request.ImportComments,
                request.ImportWorklogs,
                request.ImportChecklists,
                request.CreateMissingUsers,
                request.AutoCreateTrackers,
                request.AutoCreateStatuses,
                request.AutoCreateStatusIsClosed,
                request.AutoCreatePriorities)),
            CreatedAt = DateTime.UtcNow
        };

        dbContext.MigrationJobs.Add(job);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Persist a reusable connection (encrypted token + mappings) and link the imported
        // projects to it, so future incremental syncs (milník 3c) can run without the wizard.
        var connectionId = await connectionWriter.UpsertForEasyProjectAsync(request, cancellationToken);

        progressTracker.Init(jobId);

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var migrationService = scope.ServiceProvider.GetRequiredService<IEasyProjectMigrationService>();
                await migrationService.ExecuteAsync(jobId, request, connectionId);
            }
            catch (Exception ex)
            {
                progressTracker.Fail(jobId, ex.Message);
                progressTracker.AddLog(jobId, $"Migration failed to start: {ex.Message}");

                var progress = progressTracker.GetProgress(jobId);
                if (progress != null)
                    await notifier.NotifyProgressAsync(jobId, progress);

                // Best-effort: update job record in DB
                try
                {
                    using var dbScope = scopeFactory.CreateScope();
                    var db = dbScope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                    var failedJob = await db.MigrationJobs.FindAsync([jobId], CancellationToken.None);
                    if (failedJob != null)
                    {
                        failedJob.Status = MigrationStatus.Failed;
                        failedJob.CompletedAt = DateTime.UtcNow;
                        failedJob.ErrorLog = System.Text.Json.JsonSerializer.Serialize(new[] { ex.Message });
                        await db.SaveChangesAsync(CancellationToken.None);
                    }
                }
                catch
                {
                    // Best-effort only - tracker already has the error
                }
            }
        });

        return jobId;
    }
}
