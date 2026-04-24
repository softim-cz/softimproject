using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Infrastructure.BackgroundServices;

// Base for every recurring hosted service in this codebase. Consolidates the three
// cross-cutting concerns that each job used to wire up by hand:
//   1. Periodic tick with PeriodicTimer + cooperative cancellation
//   2. JobRun lifecycle (start/finish/error) persisted + logged with JobRunId correlation
//   3. Registration into IJobRegistry so /health/jobs can surface the job
public abstract class TrackedBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;
    private readonly TimeSpan _interval;

    protected TrackedBackgroundService(
        IServiceScopeFactory scopeFactory,
        IJobRegistry jobRegistry,
        ILogger logger,
        TimeSpan interval)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _interval = interval;
        jobRegistry.Register(JobName, interval);
    }

    protected string JobName => GetType().Name;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var recorder = scope.ServiceProvider.GetRequiredService<IJobRunRecorder>();
                await using var run = await recorder.BeginAsync(JobName, stoppingToken);

                try
                {
                    await ExecuteIterationAsync(scope.ServiceProvider, run, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    run.MarkFailure(ex);
                    _logger.LogError(ex, "{JobName} iteration threw", JobName);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    protected abstract Task ExecuteIterationAsync(
        IServiceProvider services,
        IJobRunScope run,
        CancellationToken cancellationToken);
}
