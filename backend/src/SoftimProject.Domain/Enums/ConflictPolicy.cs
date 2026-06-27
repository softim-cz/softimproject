namespace SoftimProject.Domain.Enums;

// How an incremental sync resolves a record changed in both the source and ProjectMan.
// See návrh #144 §3.5.
public enum ConflictPolicy
{
    // Default. Source owns its fields (title, status, …) and overwrites them only when the
    // source record actually changed since the last sync; ProjectMan-owned fields (AI
    // summary, watching, manual comments/worklogs) are never touched.
    SourceOwnedWins = 0,

    // Source-owned fields are overwritten on every run (ProjectMan as a pure mirror).
    StrictSourceWins = 1,

    // Any local edit locks the field against the source.
    PreserveLocalEdits = 2
}
