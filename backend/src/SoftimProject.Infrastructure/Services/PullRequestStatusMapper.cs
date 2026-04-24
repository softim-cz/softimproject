using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Infrastructure.Services;

// Convention-based PR → TaskState mapping (AC #2 "Merge PR automaticky posune ticket
// do Done"). Until the UI for per-project mapping lands, we pick states by name:
//   - PR opened  → first TaskState where name contains "review" (case-insensitive)
//   - PR merged  → first TaskState where IsClosedState == true (Done / Closed / Resolved)
// Misses are silent: if no suitable state exists, we leave the ticket on its
// current state and let the linked PR row speak for itself in the UI.
public static class PullRequestStatusMapper
{
    public static async Task<Guid?> FindReviewStateIdAsync(
        IApplicationDbContext db,
        Guid? projectTemplateId,
        CancellationToken cancellationToken)
    {
        var q = db.TaskStates.Where(ts => ts.IsActive && ts.Name.ToLower().Contains("review"));
        if (projectTemplateId.HasValue)
            q = q.Where(ts => ts.ProjectTemplateId == projectTemplateId.Value);
        return await q.OrderBy(ts => ts.SortOrder).Select(ts => (Guid?)ts.Id).FirstOrDefaultAsync(cancellationToken);
    }

    public static async Task<Guid?> FindClosedStateIdAsync(
        IApplicationDbContext db,
        Guid? projectTemplateId,
        CancellationToken cancellationToken)
    {
        var q = db.TaskStates.Where(ts => ts.IsActive && ts.IsClosedState);
        if (projectTemplateId.HasValue)
            q = q.Where(ts => ts.ProjectTemplateId == projectTemplateId.Value);
        return await q.OrderBy(ts => ts.SortOrder).Select(ts => (Guid?)ts.Id).FirstOrDefaultAsync(cancellationToken);
    }
}
