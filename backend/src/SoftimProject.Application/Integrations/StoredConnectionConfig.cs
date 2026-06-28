namespace SoftimProject.Application.Integrations;

// JSON shapes persisted on IntegrationConnection (MappingsJson / OptionsJson). They mirror
// the import settings so the scheduled incremental sync can rebuild a SyncEngineRequest
// without re-running the wizard. Keys are canonical external ids (strings) — provider-agnostic
// (EasyProject/Redmine numeric ids stringified, Jira native string ids).

public sealed record StoredConnectionMappings(
    Dictionary<string, Guid?> TrackerMapping,
    Dictionary<string, Guid> StatusMapping,
    Dictionary<string, Guid> PriorityMapping,
    Dictionary<string, Guid?> UserMapping,
    Dictionary<string, string>? AutoCreateTrackers,
    Dictionary<string, string>? AutoCreateStatuses,
    Dictionary<string, bool>? AutoCreateStatusIsClosed,
    Dictionary<string, string>? AutoCreatePriorities);

public sealed record StoredConnectionOptions(
    bool SkipClosedIssues,
    bool SkipAttachments,
    bool ImportComments,
    bool ImportWorklogs,
    bool ImportChecklists,
    bool CreateMissingUsers);
