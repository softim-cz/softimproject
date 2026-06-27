namespace SoftimProject.Domain.Enums;

// Drives how the scheduled sync treats a connection.
public enum IntegrationSyncMode
{
    // No automatic sync; runs only when triggered manually.
    Manual = 0,

    // One-time full pull, then incremental on the configured interval.
    FullThenIncremental = 1,

    // Incremental only (the initial full import already happened).
    IncrementalOnly = 2
}
