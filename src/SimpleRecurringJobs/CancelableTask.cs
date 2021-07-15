using System.Threading;
using System.Threading.Tasks;

namespace SimpleRecurringJobs
{
    public static class CancelableTask
    {
        public static Task WaitUntilCancelled(CancellationToken token)
        {
            var source = new TaskCompletionSource<bool>();
            token.Register(() => source.SetResult(true));
            return source.Task;
        }
    }
}
