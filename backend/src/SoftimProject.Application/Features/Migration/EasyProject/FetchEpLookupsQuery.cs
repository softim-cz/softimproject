using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Migration.EasyProject;

public sealed record EpTrackerMappingDto(int EpId, string EpName, Guid? SuggestedTaskTypeId, string? SuggestedTaskTypeName);
public sealed record EpStatusMappingDto(int EpId, string EpName, bool IsClosed, Guid? SuggestedTaskStateId, string? SuggestedTaskStateName);
public sealed record EpPriorityMappingDto(int EpId, string EpName, Guid? SuggestedTicketPriorityId, string? SuggestedTicketPriorityName);

public sealed record FetchEpLookupsResult(
    List<EpTrackerMappingDto> Trackers,
    List<EpStatusMappingDto> Statuses,
    List<EpPriorityMappingDto> Priorities);

public sealed record FetchEpLookupsQuery(string? BaseUrl, string? ApiKey, Guid? ConnectionId = null) : IRequest<FetchEpLookupsResult>;

public sealed class FetchEpLookupsQueryHandler(
    IEasyProjectApiClient apiClient,
    IMigrationCredentialResolver credentials,
    IApplicationDbContext dbContext) : IRequestHandler<FetchEpLookupsQuery, FetchEpLookupsResult>
{
    public async Task<FetchEpLookupsResult> Handle(FetchEpLookupsQuery request, CancellationToken cancellationToken)
    {
        var (baseUrl, apiKey) = await credentials.ResolveAsync(request.BaseUrl, request.ApiKey, request.ConnectionId, cancellationToken);
        var trackersTask = apiClient.GetTrackersAsync(baseUrl, apiKey, cancellationToken);
        var statusesTask = apiClient.GetIssueStatusesAsync(baseUrl, apiKey, cancellationToken);
        var prioritiesTask = apiClient.GetIssuePrioritiesAsync(baseUrl, apiKey, cancellationToken);

        await Task.WhenAll(trackersTask, statusesTask, prioritiesTask);

        var epTrackers = trackersTask.Result;
        var epStatuses = statusesTask.Result;
        var epPriorities = prioritiesTask.Result;

        // Auto-map trackers to existing TaskTypes by name match
        var taskTypes = await dbContext.TaskTypes.Where(t => t.IsActive).ToListAsync(cancellationToken);
        var trackerMappings = epTrackers.Select(t =>
        {
            var match = taskTypes.FirstOrDefault(tt =>
                tt.Name.Equals(t.Name, StringComparison.OrdinalIgnoreCase));
            return new EpTrackerMappingDto(t.Id, t.Name, match?.Id, match?.Name);
        }).ToList();

        // Auto-map statuses to TaskStates
        var taskStates = await dbContext.TaskStates.Where(ts => ts.IsActive).ToListAsync(cancellationToken);
        var statusMappings = epStatuses.Select(s =>
        {
            var match = MapStatusToTaskState(s.Name, s.IsClosed, taskStates);
            return new EpStatusMappingDto(s.Id, s.Name, s.IsClosed, match?.Id, match?.Name);
        }).ToList();

        // Auto-map priorities to TicketPriorities
        var ticketPriorities = await dbContext.TicketPriorities.Where(tp => tp.IsActive).ToListAsync(cancellationToken);
        var priorityMappings = epPriorities.Select(p =>
        {
            var match = MapPriorityToTicketPriority(p.Name, ticketPriorities);
            return new EpPriorityMappingDto(p.Id, p.Name, match?.Id, match?.Name);
        }).ToList();

        return new FetchEpLookupsResult(trackerMappings, statusMappings, priorityMappings);
    }

    private static Domain.Entities.TaskState? MapStatusToTaskState(string epName, bool isClosed, List<Domain.Entities.TaskState> taskStates)
    {
        if (isClosed)
            return taskStates.FirstOrDefault(ts => ts.IsClosedState);

        var lower = epName.ToLowerInvariant();

        // Try exact name match first
        var exactMatch = taskStates.FirstOrDefault(ts => ts.Name.Equals(epName, StringComparison.OrdinalIgnoreCase));
        if (exactMatch != null) return exactMatch;

        // Fuzzy match
        if (lower.Contains("new") || lower.Contains("backlog"))
            return taskStates.FirstOrDefault(ts => ts.Name.Contains("Backlog", StringComparison.OrdinalIgnoreCase))
                ?? taskStates.FirstOrDefault(ts => ts.IsDefault);
        if (lower.Contains("progress") || lower.Contains("working"))
            return taskStates.FirstOrDefault(ts => ts.Name.Contains("Progress", StringComparison.OrdinalIgnoreCase));
        if (lower.Contains("review") || lower.Contains("feedback"))
            return taskStates.FirstOrDefault(ts => ts.Name.Contains("Review", StringComparison.OrdinalIgnoreCase));
        if (lower.Contains("done") || lower.Contains("resolved"))
            return taskStates.FirstOrDefault(ts => ts.Name.Contains("Done", StringComparison.OrdinalIgnoreCase));
        if (lower.Contains("todo") || lower.Contains("ready") || lower.Contains("assigned"))
            return taskStates.FirstOrDefault(ts => ts.Name.Contains("Todo", StringComparison.OrdinalIgnoreCase)
                || ts.Name.Contains("To Do", StringComparison.OrdinalIgnoreCase));

        return taskStates.FirstOrDefault(ts => ts.IsDefault);
    }

    private static Domain.Entities.TicketPriority? MapPriorityToTicketPriority(string epName, List<Domain.Entities.TicketPriority> priorities)
    {
        // Try exact name match first
        var exactMatch = priorities.FirstOrDefault(p => p.Name.Equals(epName, StringComparison.OrdinalIgnoreCase));
        if (exactMatch != null) return exactMatch;

        var lower = epName.ToLowerInvariant();
        if (lower.Contains("critical") || lower.Contains("urgent") || lower.Contains("immediate"))
            return priorities.FirstOrDefault(p => p.Name.Contains("Critical", StringComparison.OrdinalIgnoreCase));
        if (lower.Contains("high"))
            return priorities.FirstOrDefault(p => p.Name.Contains("High", StringComparison.OrdinalIgnoreCase));
        if (lower.Contains("low"))
            return priorities.FirstOrDefault(p => p.Name.Contains("Low", StringComparison.OrdinalIgnoreCase));

        return priorities.FirstOrDefault(p => p.Name.Contains("Medium", StringComparison.OrdinalIgnoreCase))
            ?? priorities.FirstOrDefault(p => p.IsDefault);
    }
}
