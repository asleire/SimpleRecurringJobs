using System;
using System.Collections.Generic;
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
    private readonly JobMetrics _jobMetrics;

    public JobsExecutor(IJobStore store, IJobLogger logger, IJobClock clock, JobMetrics jobMetrics)
    {
        _store = store;
        _clock = clock;
        _jobMetrics = jobMetrics;
        _logger = logger.ForSource<JobsExecutor>();
    }

    public async Task<bool> Execute(IJob job, CancellationToken cancellationToken)
    {
        var instanceId = Guid.NewGuid().ToString("N");
        using var activity = _activitySource.StartActivity($"Job: {job.Id}");
        activity?.SetTag("InstanceId", instanceId);

        var sw = Stopwatch.StartNew();
        string lockStatus = "unknown";
        IJobLock? @lock;

        try
        {
            _jobMetrics.JobLockAttemptStarted.Add(1, new KeyValuePair<string, object?>("id", job.Id));

            @lock = await _store.TryLock(job, instanceId);

            if (@lock == null)
            {
                lockStatus = "missed";
                return false;
            }
            lockStatus = "acquired";
        }
        catch (Exception)
        {
            lockStatus = "error";
            throw;
        }
        finally
        {
            _jobMetrics.JobLockAttemptCompleted.Add(1, new ("id", job.Id), new ("status", lockStatus));
            _jobMetrics.JobLockAttemptDurationInMs.Add(sw.ElapsedMilliseconds, new ("id", job.Id), new ("status", lockStatus));
        }

        await using var _ = @lock;

        var info = await _store.Retrieve(job);

        info.LastTriggered = _clock.UtcNow;
        await _store.Save(info);

        string status = "unknown";

        try
        {
            _logger.LogVerbose("Executing job {JobId}", job.Id);
            _jobMetrics.JobStarted.Add(1, new KeyValuePair<string, object?>("id", job.Id));
            sw.Restart();
            await job.Execute(cancellationToken);
            status = "completed";
            sw.Stop();
            _logger.LogVerbose("Execution of {JobId} completed in {ElapsedMs}ms", job.Id, sw.ElapsedMilliseconds);
            info.LastSuccess = _clock.UtcNow;
            return true;
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException && cancellationToken.IsCancellationRequested)
            {
                _logger.LogInfo(ex, "Job {JobId} was cancelled", job.Id);
                status = "cancelled";
            }
            else
            {
                _logger.LogError(ex, "Job {JobId} terminated unexpectedly", job.Id);
                status = "error";
            }

            info.LastFailure = _clock.UtcNow;
            return false;
        }
        finally
        {
            _jobMetrics.JobCompleted.Add(1, new ("id", job.Id), new ("status", status));
            _jobMetrics.JobDurationInMs.Add(sw.ElapsedMilliseconds, new ("id", job.Id), new ("status", status));
            await _store.Save(info);
        }
    }
}
