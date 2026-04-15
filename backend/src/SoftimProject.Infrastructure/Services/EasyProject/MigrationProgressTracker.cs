using System.Collections.Concurrent;
using SoftimProject.Application.Features.Migration.EasyProject.Dtos;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Infrastructure.Services.EasyProject;

public sealed class MigrationProgressTracker : IMigrationProgressTracker
{
    private readonly ConcurrentDictionary<Guid, MigrationState> _states = new();

    public void Init(Guid jobId)
    {
        _states[jobId] = new MigrationState();
    }

    public void UpdatePhase(Guid jobId, string phase)
    {
        if (_states.TryGetValue(jobId, out var state))
            state.CurrentPhase = phase;
    }

    public void UpdateCounts(Guid jobId, string entity, int total, int migrated)
    {
        if (!_states.TryGetValue(jobId, out var state)) return;
        switch (entity.ToLowerInvariant())
        {
            case "projects": state.ProjectsTotal = total; state.ProjectsMigrated = migrated; break;
            case "tickets": state.TicketsTotal = total; state.TicketsMigrated = migrated; break;
            case "comments": state.CommentsTotal = total; state.CommentsMigrated = migrated; break;
            case "worklogs": state.WorklogsTotal = total; state.WorklogsMigrated = migrated; break;
            case "attachments": state.AttachmentsTotal = total; state.AttachmentsMigrated = migrated; break;
        }
    }

    public void IncrementCreated(Guid jobId)
    {
        if (_states.TryGetValue(jobId, out var state))
            Interlocked.Increment(ref state.ItemsCreated);
    }

    public void IncrementUpdated(Guid jobId)
    {
        if (_states.TryGetValue(jobId, out var state))
            Interlocked.Increment(ref state.ItemsUpdated);
    }

    public void IncrementSkipped(Guid jobId)
    {
        if (_states.TryGetValue(jobId, out var state))
            Interlocked.Increment(ref state.ItemsSkipped);
    }

    public void IncrementFailed(Guid jobId)
    {
        if (_states.TryGetValue(jobId, out var state))
            Interlocked.Increment(ref state.ErrorCount);
    }

    public void AddError(Guid jobId, string error)
    {
        if (!_states.TryGetValue(jobId, out var state)) return;
        Interlocked.Increment(ref state.ErrorCount);
        lock (state.RecentErrors)
        {
            state.RecentErrors.Add(error);
            if (state.RecentErrors.Count > 20)
                state.RecentErrors.RemoveAt(0);
        }
    }

    public void AddLog(Guid jobId, string message)
    {
        if (!_states.TryGetValue(jobId, out var state)) return;
        lock (state.RecentLog)
        {
            state.RecentLog.Add($"[{DateTime.UtcNow:HH:mm:ss}] {message}");
            if (state.RecentLog.Count > 50)
                state.RecentLog.RemoveAt(0);
        }
    }

    public void Complete(Guid jobId, bool hasErrors)
    {
        if (_states.TryGetValue(jobId, out var state))
        {
            state.Status = hasErrors ? "CompletedWithErrors" : "Completed";
            state.CurrentPhase = "Done";
        }
    }

    public void Fail(Guid jobId, string error)
    {
        if (_states.TryGetValue(jobId, out var state))
        {
            state.Status = "Failed";
            state.CurrentPhase = "Failed";
            AddError(jobId, error);
        }
    }

    public void Cancel(Guid jobId)
    {
        if (_states.TryGetValue(jobId, out var state))
        {
            state.Status = "Cancelled";
            state.CurrentPhase = "Cancelled";
        }
    }

    public MigrationProgressDto? GetProgress(Guid jobId)
    {
        if (!_states.TryGetValue(jobId, out var state)) return null;

        var totalItems = state.ProjectsTotal + state.TicketsTotal + state.CommentsTotal
            + state.WorklogsTotal + state.AttachmentsTotal;
        var migratedItems = state.ProjectsMigrated + state.TicketsMigrated + state.CommentsMigrated
            + state.WorklogsMigrated + state.AttachmentsMigrated;
        var percent = totalItems > 0 ? (int)(migratedItems * 100.0 / totalItems) : 0;

        List<string> errors, logs;
        lock (state.RecentErrors) { errors = [.. state.RecentErrors]; }
        lock (state.RecentLog) { logs = [.. state.RecentLog]; }

        return new MigrationProgressDto(
            jobId,
            state.Status,
            state.CurrentPhase,
            state.ProjectsTotal, state.ProjectsMigrated,
            state.TicketsTotal, state.TicketsMigrated,
            state.CommentsTotal, state.CommentsMigrated,
            state.WorklogsTotal, state.WorklogsMigrated,
            state.AttachmentsTotal, state.AttachmentsMigrated,
            state.ErrorCount,
            state.ItemsCreated,
            state.ItemsUpdated,
            state.ItemsSkipped,
            errors, logs, percent);
    }

    public CancellationToken GetCancellationToken(Guid jobId)
    {
        if (_states.TryGetValue(jobId, out var state))
            return state.Cts.Token;
        return CancellationToken.None;
    }

    public void RequestCancellation(Guid jobId)
    {
        if (_states.TryGetValue(jobId, out var state))
            state.Cts.Cancel();
    }

    private sealed class MigrationState
    {
        public string Status = "Running";
        public string CurrentPhase = "Initializing";
        public int ProjectsTotal, ProjectsMigrated;
        public int TicketsTotal, TicketsMigrated;
        public int CommentsTotal, CommentsMigrated;
        public int WorklogsTotal, WorklogsMigrated;
        public int AttachmentsTotal, AttachmentsMigrated;
        public int ErrorCount;
        public int ItemsCreated, ItemsUpdated, ItemsSkipped;
        public List<string> RecentErrors = [];
        public List<string> RecentLog = [];
        public CancellationTokenSource Cts = new();
    }
}
