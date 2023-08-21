using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleRecurringJobs;

/// <summary>
///     Takes care of executing a single instance of a job.
/// </summary>
internal class JobsExecutor : IJobExecutor
{
    private readonly IJobClock _clock;
    private readonly IJobLogger _logger;
    private readonly IJobStore _store;
    private readonly ActivitySource _activitySource = new ("SimpleRecurringJobs");

    public JobsExecutor(IJobStore store, IJobLogger logger, IJobClock clock)
    {
        _store = store;
        _clock = clock;
        _logger = logger.ForSource<JobsExecutor>();
    }

    public async Task<bool> Execute(IJob job, CancellationToken cancellationToken)
    {
        var instanceId = Guid.NewGuid().ToString("N");
        using var activity = _activitySource.StartActivity($"Job: {job.Id}");
        activity?.SetTag("InstanceId", instanceId);
        
        var @lock = await _store.TryLock(job, instanceId);

        if (@lock == null)
            return false;

        await using var _ = @lock;

        var info = await _store.Retrieve(job);

        info.LastTriggered = _clock.UtcNow;
        await _store.Save(info);

        try
        {
            var sw = Stopwatch.StartNew();
            _logger.LogVerbose("Executing job {JobId}", job.Id);
            await job.Execute(cancellationToken);
            sw.Stop();
            _logger.LogVerbose("Execution of {JobId} completed in {ElapsedMs}ms", job.Id, sw.ElapsedMilliseconds);
            info.LastSuccess = _clock.UtcNow;
            await _store.Save(info);
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException && cancellationToken.IsCancellationRequested)
            {
                _logger.LogInfo(ex, "Job {JobId} was cancelled", job.Id);   
            }
            else
            {
                _logger.LogError(ex, "Job {JobId} terminated unexpectedly", job.Id);
            }

            info.LastFailure = _clock.UtcNow;
            await _store.Save(info);
            return false;
        }

        return true;
    }
}
