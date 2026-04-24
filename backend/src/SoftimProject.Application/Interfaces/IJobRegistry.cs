namespace SoftimProject.Application.Interfaces;

// Lightweight in-memory catalog populated by each TrackedBackgroundService on startup.
// The /health/jobs endpoint reads this to know which jobs exist and how often they're
// expected to tick; it then looks up the latest JobRun per name to compute staleness.
public interface IJobRegistry
{
    void Register(string jobName, TimeSpan expectedInterval);
    IReadOnlyCollection<JobRegistration> List();
}

public sealed record JobRegistration(string JobName, TimeSpan ExpectedInterval);
