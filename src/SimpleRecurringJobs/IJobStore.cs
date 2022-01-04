using System;
using System.Threading.Tasks;

namespace SimpleRecurringJobs;

/// <summary>
///     Provides a simple store for persisting <see cref="JobInfo" /> data and Job execution locks.
/// </summary>
public interface IJobStore
{
    /// <summary>
    ///     Attempts to acquire a lock on the job using the specified jobInstanceId.
    /// </summary>
    /// <param name="job"></param>
    /// <param name="jobInstanceId">Unique ID for the job instance.</param>
    /// <returns>Whether a lock was acquired.</returns>
    Task<IJobLock?> TryLock(IJob job, string jobInstanceId);

    /// <summary>
    ///     Retrieves the currently persisted JobInfo for the given jobId.
    /// </summary>
    /// <param name="job"></param>
    /// <returns></returns>
    Task<JobInfo> Retrieve(IJob job);

    /// <summary>
    ///     Saves the given info object.
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    Task Save(JobInfo info);
}

public interface IJobLock : IAsyncDisposable
{
    Task Refresh();
}