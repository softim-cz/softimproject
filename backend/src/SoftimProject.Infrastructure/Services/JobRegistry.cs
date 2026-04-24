using System.Collections.Concurrent;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Infrastructure.Services;

public sealed class JobRegistry : IJobRegistry
{
    private readonly ConcurrentDictionary<string, JobRegistration> _jobs = new(StringComparer.Ordinal);

    public void Register(string jobName, TimeSpan expectedInterval)
    {
        _jobs[jobName] = new JobRegistration(jobName, expectedInterval);
    }

    public IReadOnlyCollection<JobRegistration> List() => _jobs.Values.ToList();
}
