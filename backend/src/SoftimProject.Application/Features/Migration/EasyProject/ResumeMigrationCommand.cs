using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Migration.EasyProject;

// Re-runs a failed or cancelled migration job using the Configuration JSON we
// stored on the original job. The admin re-supplies the ApiKey (kept out of
// storage intentionally — rotatable secret). Fáze completed by the previous run
// are logged and skipped thanks to the idempotent ExternalId upserts inside
// EasyProjectMigrationService; CurrentPhase is the visible indicator of progress.
public sealed record ResumeMigrationCommand(Guid JobId, string ApiKey) : IRequest<Guid>, IRequireRole
{
    public string RequiredRole => "Admin";
}

public sealed class ResumeMigrationCommandValidator : AbstractValidator<ResumeMigrationCommand>
{
    public ResumeMigrationCommandValidator()
    {
        RuleFor(x => x.ApiKey).NotEmpty();
    }
}

public sealed class ResumeMigrationCommandHandler(
    IApplicationDbContext dbContext,
    IMigrationProgressTracker progressTracker,
    IMigrationNotifier notifier,
    IServiceScopeFactory scopeFactory) : IRequestHandler<ResumeMigrationCommand, Guid>
{
    public async Task<Guid> Handle(ResumeMigrationCommand request, CancellationToken cancellationToken)
    {
        var job = await dbContext.MigrationJobs
            .FirstOrDefaultAsync(j => j.Id == request.JobId, cancellationToken)
            ?? throw new NotFoundException(nameof(MigrationJob), request.JobId);

        if (job.Status is not (MigrationStatus.Failed or MigrationStatus.Cancelled or MigrationStatus.CompletedWithErrors))
        {
            throw new ValidationException(
                $"Only Failed / Cancelled / CompletedWithErrors jobs can be resumed (current status: {job.Status}).");
        }

        if (string.IsNullOrWhiteSpace(job.Configuration))
        {
            throw new ValidationException("Job has no stored configuration to replay from.");
        }

        var config = System.Text.Json.JsonSerializer.Deserialize<StoredMigrationConfig>(job.Configuration)
            ?? throw new ValidationException("Job configuration is corrupted and cannot be parsed.");

        var rebuilt = new StartMigrationCommand(
            config.BaseUrl,
            request.ApiKey,
            config.ProjectIds,
            config.TargetProjectTemplateId,
            config.TrackerMapping,
            config.StatusMapping,
            config.PriorityMapping,
            config.UserMapping,
            config.SkipClosedIssues,
            config.SkipAttachments,
            config.ImportComments,
            config.ImportWorklogs,
            config.ImportChecklists,
            config.CreateMissingUsers,
            config.AutoCreateTrackers,
            config.AutoCreateStatuses,
            config.AutoCreateStatusIsClosed,
            config.AutoCreatePriorities);

        // Reset only the run-state fields — keep CurrentPhase so ExecuteAsync can
        // skip the boundaries that already finished.
        job.Status = MigrationStatus.Pending;
        job.StartedAt = DateTime.UtcNow;
        job.CompletedAt = null;
        job.ErrorLog = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        progressTracker.Init(request.JobId);
        progressTracker.AddLog(request.JobId,
            $"Resuming job — last completed phase: {job.CurrentPhase}.");

        // Same fire-and-forget pattern as StartMigrationCommandHandler, so the
        // HTTP caller gets the job id back immediately and polls progress via
        // GetMigrationProgressQuery / SignalR.
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var migrationService = scope.ServiceProvider.GetRequiredService<IEasyProjectMigrationService>();
                await migrationService.ExecuteAsync(request.JobId, rebuilt);
            }
            catch (Exception ex)
            {
                progressTracker.Fail(request.JobId, ex.Message);
                var progress = progressTracker.GetProgress(request.JobId);
                if (progress != null)
                    await notifier.NotifyProgressAsync(request.JobId, progress);
                try
                {
                    using var dbScope = scopeFactory.CreateScope();
                    var db = dbScope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                    var failedJob = await db.MigrationJobs.FindAsync([request.JobId], CancellationToken.None);
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
                    // Best-effort — tracker carries the error too.
                }
            }
        });

        return request.JobId;
    }
}
