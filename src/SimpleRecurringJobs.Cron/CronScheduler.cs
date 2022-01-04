using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cronos;

namespace SimpleRecurringJobs.Cron;

internal class CronScheduler : IJobScheduler
{
    private readonly IAsyncDelayer _delayer;
    private readonly IJobClock _clock;
    private readonly DateTime _startedAt;

    public CronScheduler(IAsyncDelayer delayer, IJobClock clock)
    {
        _delayer = delayer;
        _clock = clock;
        _startedAt = clock.UtcNow;
    }

    public bool CanHandle(IJob job) => job is ICronJob;

    public async Task<bool> WaitUntilNext(IJob job, JobInfo jobInfo, CancellationToken cancellationToken)
    {
        if (job is not ICronJob cronJob)
        {
            throw new ArgumentException(
                $"{nameof(CronScheduler)} only supports jobs of type {nameof(ICronJob)}",
                nameof(job)
            );
        }

        var timestampToCheck = cronJob.Mode == TriggerCountMode.CountSinceLastTrigger
            ? jobInfo.LastTriggered
            : new[] {jobInfo.LastTriggered, jobInfo.LastFailure, jobInfo.LastSuccess}.Max();

        if ((timestampToCheck - DateTime.MinValue).TotalSeconds == 0)
        {
            // For the very first execution of a job we want to wait until the next cron occurrence since the job was first registered (which is now).
            timestampToCheck = _startedAt;
        }

        var nextOccurrence = cronJob.Cron.GetNextOccurrence(timestampToCheck);

        if (nextOccurrence == null)
        {
            throw new InvalidOperationException("CRON Expression has no more occurrences.");
        }

        var now = _clock.UtcNow;

        if (nextOccurrence.Value > now)
        {
            var timeUntilNextTrigger = nextOccurrence.Value - now;
            var task = _delayer.Delay(timeUntilNextTrigger, cancellationToken);
            await task;

            // We return 'false' now so that the worker will call this method again with an up-to-date JobInfo.
            return false;
        }

        return true;
    }
}