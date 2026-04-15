using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Migration.EasyProject;

public sealed record StartMigrationCommand(
    string BaseUrl,
    string ApiKey,
    List<int> ProjectIds,
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

public sealed class StartMigrationCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IMigrationProgressTracker progressTracker,
    IMigrationNotifier notifier,
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
            Configuration = System.Text.Json.JsonSerializer.Serialize(new
            {
                request.ProjectIds,
                request.SkipClosedIssues,
                request.SkipAttachments,
                request.ImportComments,
                request.ImportWorklogs,
                request.ImportChecklists,
                request.CreateMissingUsers
            }),
            CreatedAt = DateTime.UtcNow
        };

        dbContext.MigrationJobs.Add(job);
        await dbContext.SaveChangesAsync(cancellationToken);

        progressTracker.Init(jobId);

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var migrationService = scope.ServiceProvider.GetRequiredService<IEasyProjectMigrationService>();
                await migrationService.ExecuteAsync(jobId, request);
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
