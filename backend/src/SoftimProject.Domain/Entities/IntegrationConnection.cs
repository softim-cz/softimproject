using SoftimProject.Domain.Common;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Domain.Entities;

/// <summary>
/// A persistent connection to an external project system (EasyProject, Jira, Redmine, …).
/// Holds the credentials, mapping and scheduling needed to run repeated, incremental syncs.
/// See návrh #144 §3.2.
/// </summary>
public class IntegrationConnection : BaseEntity
{
    /// <summary>Human-readable label (e.g. "Acme EasyProject").</summary>
    public string Name { get; set; } = string.Empty;

    public SyncType SourceSystem { get; set; }

    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>API token encrypted at rest via <c>ISecretProtector</c> (never stored or returned in plaintext).</summary>
    public string? EncryptedApiToken { get; set; }

    /// <summary>Shared secret used to verify inbound webhook signatures.</summary>
    public string? WebhookSecret { get; set; }

    /// <summary>Template applied to projects created by this connection.</summary>
    public Guid TargetProjectTemplateId { get; set; }

    /// <summary>Customer the imported projects belong to (návrh #144 §9).</summary>
    public Guid? TargetCompanyId { get; set; }

    /// <summary>User attributed as initiator of headless scheduled syncs (reporter fallback, PM assignment).</summary>
    public Guid CreatedByUserId { get; set; }

    public ConflictPolicy ConflictPolicy { get; set; } = ConflictPolicy.SourceOwnedWins;

    public IntegrationSyncMode Mode { get; set; } = IntegrationSyncMode.Manual;

    /// <summary>Sync interval in minutes. Default 24 h; minimum 1 h (enforced on write).</summary>
    public int IntervalMinutes { get; set; } = 1440;

    public bool IsEnabled { get; set; }

    /// <summary>Start of the last sync run (drives the "is it due" check).</summary>
    public DateTime? LastSyncStartedAt { get; set; }

    /// <summary>High-water mark — the newest source "updated" timestamp successfully pulled.</summary>
    public DateTime? LastSyncWatermark { get; set; }

    /// <summary>JSON: per-connection flags (SkipClosed, ImportComments/Worklogs/Checklists, CreateMissingUsers, …).</summary>
    public string? OptionsJson { get; set; }

    /// <summary>JSON: persisted status/priority/tracker/user mappings, reused by every incremental run.</summary>
    public string? MappingsJson { get; set; }

    /// <summary>JSON: which external projects are synced (and onto which internal project).</summary>
    public string? ProjectSelectorJson { get; set; }

    // Navigation
    public Company? TargetCompany { get; set; }
    public ProjectTemplate TargetProjectTemplate { get; set; } = null!;
    public ICollection<Project> Projects { get; set; } = new List<Project>();
}
