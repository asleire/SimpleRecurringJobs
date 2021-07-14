using System.Threading;
using System.Threading.Tasks;

namespace SimpleRecurringJobs
{
    public interface IJobExecutor
    {
        /// <summary>
        ///     Attempts to execute the job, creating a lock and logging eventual errors.
        /// </summary>
        /// <param name="job"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> Execute(IJob job, CancellationToken cancellationToken);
    }
}