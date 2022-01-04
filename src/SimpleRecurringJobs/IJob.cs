using System.Threading;
using System.Threading.Tasks;

namespace SimpleRecurringJobs;

public interface IJob
{
    /// <summary>
    ///     Unique ID for the job.
    /// </summary>
    string Id { get; }

    /// <summary>
    ///     The code to execute for the job.
    /// </summary>
    /// <returns></returns>
    Task Execute(CancellationToken cancellationToken);
}
