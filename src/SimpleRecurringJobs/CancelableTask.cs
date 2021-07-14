using System.Threading;
using System.Threading.Tasks;

namespace SimpleRecurringJobs
{
    public static class CancelableTask
    {
        public static Task WaitUntilCancelled(CancellationToken token)
        {
            var source = new TaskCompletionSource();
            token.Register(() => source.SetResult());
            return source.Task;
        }
    }
}