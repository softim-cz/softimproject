namespace SoftimProject.Application.Features.Migration.EasyProject;

// Shape of `MigrationJob.Configuration` — everything `StartMigrationCommand` carries
// except the ApiKey. Kept as a public record so the ResumeMigrationCommand handler
// can deserialize and rebuild the original command.
public sealed record StoredMigrationConfig(
    string BaseUrl,
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
    Dictionary<int, string>? AutoCreateTrackers,
    Dictionary<int, string>? AutoCreateStatuses,
    Dictionary<int, bool>? AutoCreateStatusIsClosed,
    Dictionary<int, string>? AutoCreatePriorities);
