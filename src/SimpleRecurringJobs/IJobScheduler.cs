using System.Threading;
using System.Threading.Tasks;

namespace SimpleRecurringJobs
{
    /// <summary>
    ///     Provides a way to let us know when a job should next execute.
    /// </summary>
    public interface IJobScheduler
    {
        /// <summary>
        ///     Whether this scheduler can handle this kind of job.
        /// </summary>
        /// <param name="job"></param>
        /// <returns></returns>
        bool CanHandle(IJob job);

        /// <summary>
        ///     Returns a task that blocks until job should be executed.
        /// </summary>
        /// <param name="job"></param>
        /// <param name="jobInfo"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Whether job should be run now.</returns>
        Task<bool> WaitUntilNext(IJob job, JobInfo jobInfo, CancellationToken cancellationToken);
    }
}
