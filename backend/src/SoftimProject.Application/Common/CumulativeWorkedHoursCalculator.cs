using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Common;

/// <summary>
/// Maintains <c>Ticket.CumulativeWorkedHours</c> as the recursive sum of a ticket's own
/// worklog hours plus the cumulative hours of all its sub-tickets.
///
/// The stored column lets list/detail/export queries read the rolled-up value cheaply.
/// We keep it in sync with a triggered recompute: every worklog mutation (and ticket
/// re-parenting) recomputes the affected ticket and walks up its ancestor chain.
/// </summary>
public static class CumulativeWorkedHoursCalculator
{
    // Defensive bound against a corrupt parent chain; real ticket trees are shallow.
    private const int MaxDepth = 1000;

    /// <summary>
    /// Recomputes <c>CumulativeWorkedHours</c> for the given ticket and every ancestor up to
    /// the root, persisting each level before moving up so the parent sees fresh child values.
    /// Relies on the invariant that sibling subtrees outside the walked chain are already correct.
    /// </summary>
    public static async Task RecalculateUpwardAsync(
        IApplicationDbContext dbContext,
        Guid? ticketId,
        CancellationToken cancellationToken = default)
    {
        var visited = new HashSet<Guid>();
        var current = ticketId;

        while (current.HasValue && visited.Add(current.Value) && visited.Count <= MaxDepth)
        {
            var ticket = await dbContext.Tickets
                .FirstOrDefaultAsync(t => t.Id == current.Value, cancellationToken);
            if (ticket is null)
            {
                return;
            }

            var ownHours = await dbContext.Worklogs
                .Where(w => w.TicketId == ticket.Id)
                .SumAsync(w => (decimal?)w.Hours, cancellationToken) ?? 0m;

            var childrenHours = await dbContext.Tickets
                .Where(t => t.ParentTicketId == ticket.Id)
                .SumAsync(t => (decimal?)t.CumulativeWorkedHours, cancellationToken) ?? 0m;

            var next = ownHours + childrenHours;
            if (ticket.CumulativeWorkedHours != next)
            {
                ticket.CumulativeWorkedHours = next;
                // Persist before recomputing the parent, whose children-sum query reads from the DB.
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            current = ticket.ParentTicketId;
        }
    }

    /// <summary>
    /// Full bottom-up recompute for every ticket in a project. Used after a bulk import where
    /// no incremental invariant has been maintained yet.
    /// </summary>
    public static async Task RecalculateProjectAsync(
        IApplicationDbContext dbContext,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var tickets = await dbContext.Tickets
            .Where(t => t.ProjectId == projectId)
            .ToListAsync(cancellationToken);

        var ownHours = (await dbContext.Worklogs
                .Where(w => w.Ticket.ProjectId == projectId)
                .GroupBy(w => w.TicketId)
                .Select(g => new { TicketId = g.Key, Sum = g.Sum(w => w.Hours) })
                .ToListAsync(cancellationToken))
            .ToDictionary(x => x.TicketId, x => x.Sum);

        var childrenByParent = tickets
            .Where(t => t.ParentTicketId.HasValue)
            .GroupBy(t => t.ParentTicketId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(t => t.Id).ToList());

        var memo = new Dictionary<Guid, decimal>();

        decimal Subtree(Guid id, HashSet<Guid> stack)
        {
            if (memo.TryGetValue(id, out var cached))
            {
                return cached;
            }
            if (!stack.Add(id))
            {
                return 0m; // cycle guard
            }

            var sum = ownHours.GetValueOrDefault(id, 0m);
            if (childrenByParent.TryGetValue(id, out var children))
            {
                foreach (var childId in children)
                {
                    sum += Subtree(childId, stack);
                }
            }

            stack.Remove(id);
            memo[id] = sum;
            return sum;
        }

        foreach (var ticket in tickets)
        {
            ticket.CumulativeWorkedHours = Subtree(ticket.Id, new HashSet<Guid>());
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
