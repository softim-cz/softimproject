namespace SoftimProject.Application.Integrations;

// JSON shapes persisted on IntegrationConnection (MappingsJson / OptionsJson). They mirror
// the EasyProject migration command's settings so the scheduled incremental sync (milník 3c)
// can rebuild a SyncEngineRequest without re-running the wizard. External (EP) ids are ints.

public sealed record StoredConnectionMappings(
    Dictionary<int, Guid?> TrackerMapping,
    Dictionary<int, Guid> StatusMapping,
    Dictionary<int, Guid> PriorityMapping,
    Dictionary<int, Guid?> UserMapping,
    Dictionary<int, string>? AutoCreateTrackers,
    Dictionary<int, string>? AutoCreateStatuses,
    Dictionary<int, bool>? AutoCreateStatusIsClosed,
    Dictionary<int, string>? AutoCreatePriorities);

public sealed record StoredConnectionOptions(
    bool SkipClosedIssues,
    bool SkipAttachments,
    bool ImportComments,
    bool ImportWorklogs,
    bool ImportChecklists,
    bool CreateMissingUsers);
