using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleRecurringJobs.Interval
{
    public class IntervalScheduler : IJobScheduler
    {
        private readonly IJobClock _clock;
        private readonly IAsyncDelayer _delayer;

        public IntervalScheduler(IJobClock clock, IAsyncDelayer delayer)
        {
            _clock = clock;
            _delayer = delayer;
        }

        public bool CanHandle(IJob job) => job is IIntervalJob;

        public async Task<bool> WaitUntilNext(IJob job, JobInfo jobInfo, CancellationToken cancellationToken)
        {
            if (job is not IIntervalJob intervalJob)
            {
                throw new ArgumentException(
                    $"{nameof(IntervalScheduler)} only supports jobs of type {nameof(IIntervalJob)}",
                    nameof(job)
                );
            }

            var timestampToCheck = intervalJob.Mode == TriggerCountMode.CountSinceLastTrigger
                ? jobInfo.LastTriggered
                : new[] {jobInfo.LastTriggered, jobInfo.LastFailure, jobInfo.LastSuccess}.Max();

            var timeSinceLastTrigger = _clock.UtcNow - timestampToCheck;

            if (timeSinceLastTrigger < intervalJob.Interval)
            {
                var timeUntilNextTrigger = intervalJob.Interval - timeSinceLastTrigger;
                var task = _delayer.Delay(timeUntilNextTrigger, cancellationToken);
                await task;

                // We return 'false' now so that the worker will call this method again with an up-to-date JobInfo.
                return false;
            }

            return true;
        }
    }
}
