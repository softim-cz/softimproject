using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Health;

public sealed record JobHealthDto(
    string JobName,
    DateTime? LastRunAt,
    JobRunStatus? LastStatus,
    long? LastDurationMs,
    int? LastItemsProcessed,
    int? LastItemsFailed,
    string? LastError,
    DateTime? ExpectedNextRunAt,
    bool IsOverdue);

public sealed record JobsHealthDto(string Status, IReadOnlyList<JobHealthDto> Jobs);

// Not gated behind an auth marker on purpose — /health endpoints are typically probed
// by infra without credentials. Anyone who can reach the API gets the overview.
public sealed record GetJobsHealthQuery : IRequest<JobsHealthDto>;

public sealed class GetJobsHealthQueryHandler(
    IApplicationDbContext dbContext,
    IJobRegistry jobRegistry) : IRequestHandler<GetJobsHealthQuery, JobsHealthDto>
{
    public async Task<JobsHealthDto> Handle(GetJobsHealthQuery request, CancellationToken cancellationToken)
    {
        var registrations = jobRegistry.List();
        var now = DateTime.UtcNow;

        var registeredNames = registrations.Select(r => r.JobName).ToList();
        // One query: newest run per JobName among registered jobs.
        var latestRuns = await dbContext.JobRuns
            .Where(r => registeredNames.Contains(r.JobName))
            .GroupBy(r => r.JobName)
            .Select(g => g
                .OrderByDescending(r => r.StartedAt)
                .Select(r => new
                {
                    r.JobName,
                    r.StartedAt,
                    r.CompletedAt,
                    r.Status,
                    r.DurationMs,
                    r.ItemsProcessed,
                    r.ItemsFailed,
                    r.ErrorMessage,
                })
                .First())
            .ToListAsync(cancellationToken);

        var byName = latestRuns.ToDictionary(r => r.JobName);
        var jobs = registrations
            .OrderBy(r => r.JobName, StringComparer.Ordinal)
            .Select(reg =>
            {
                byName.TryGetValue(reg.JobName, out var last);

                // Overdue = no run has been seen for more than 2× expected interval. The 2× grace
                // absorbs tick skew (PeriodicTimer can drift under load) and scheduled-idle slots
                // (DeadlineNotificationService ticks hourly but only works once a day).
                var overdue = last is null
                    || (now - last.StartedAt) > reg.ExpectedInterval * 2;

                var nextRunAt = last is null ? null : (DateTime?)last.StartedAt.Add(reg.ExpectedInterval);

                return new JobHealthDto(
                    reg.JobName,
                    last?.StartedAt,
                    last?.Status,
                    last?.DurationMs,
                    last?.ItemsProcessed,
                    last?.ItemsFailed,
                    last?.ErrorMessage,
                    nextRunAt,
                    overdue);
            })
            .ToList();

        var degraded = jobs.Any(j => j.IsOverdue || j.LastStatus == JobRunStatus.Failed);
        return new JobsHealthDto(degraded ? "Degraded" : "Healthy", jobs);
    }
}
