using Cronos;

namespace SimpleRecurringJobs.Cron;

public interface ICronJob : IJob
{
    /// <summary>
    /// CRON expression describing the job's schedule.
    /// </summary>
    public CronExpression Cron { get; }

    /// <summary>
    ///     How to determine the timestamp of the previous job execution.
    /// </summary>
    public TriggerCountMode Mode { get; }
}