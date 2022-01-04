using System;

namespace SimpleRecurringJobs.Interval;

/// <summary>
///     A simple job that will run with a given interval. Does not care about time of day.
/// </summary>
public interface IIntervalJob : IJob
{
    /// <summary>
    ///     How long between each job.
    /// </summary>
    public TimeSpan Interval { get; }

    /// <summary>
    ///     How to determine the timestamp of the previous job execution.
    /// </summary>
    public TriggerCountMode Mode { get; }
}