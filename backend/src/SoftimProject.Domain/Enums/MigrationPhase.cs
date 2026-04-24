namespace SoftimProject.Domain.Enums;

// Ordered steps of `EasyProjectMigrationService.ExecuteAsync`. Stored on
// `MigrationJob.CurrentPhase` so a resumed job can skip the phases that already
// completed. Each phase is idempotent (upserts by ExternalId) — re-running a
// completed phase is safe but wasteful, hence the skip.
public enum MigrationPhase
{
    Pending = 0,
    Fetching = 1,
    Lookups = 2,
    Users = 3,
    Projects = 4,
    Tickets = 5,
    Comments = 6,
    Worklogs = 7,
    CustomFields = 8,
    Checklists = 9,
    Attachments = 10,
    Recalculating = 11,
    Done = 12
}
