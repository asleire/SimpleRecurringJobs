using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleRecurringJobs.Tests
{
    /// <summary>
    ///     Allows using a MockClock to perform the equivalent of Task.Delay
    /// </summary>
    public class MockDelayer : IAsyncDelayer
    {
        private readonly object _lock = new();
        private readonly Dictionary<Guid, WaitObject> _waitObjects = new();

        public MockDelayer(MockClock clock)
        {
            clock.TimeChanged += MoveTime;
        }

        public async Task Delay(TimeSpan timeSpan, CancellationToken cancellationToken)
        {
            if (timeSpan == TimeSpan.Zero)
                return;

            var waitObject = new WaitObject
            {
                TimeRemaining = timeSpan
            };

            lock (_lock)
            {
                _waitObjects.Add(waitObject.Id, waitObject);
            }

            await Task.WhenAny(waitObject.CompletionSource.Task, CancelableTask.WaitUntilCancelled(cancellationToken));
        }

        private void MoveTime(TimeSpan time)
        {
            lock (_lock)
            {
                var expired = new List<Guid>();
                foreach (var (id, obj) in _waitObjects)
                    if (time >= obj.TimeRemaining)
                        expired.Add(id);
                    else
                        obj.TimeRemaining -= time;

                foreach (var exp in expired)
                    if (_waitObjects.Remove(exp, out var waitObj))
                        waitObj.CompletionSource.SetResult();
            }
        }

        private class WaitObject
        {
            public Guid Id { get; } = Guid.NewGuid();

            public TimeSpan TimeRemaining { get; set; }

            public TaskCompletionSource CompletionSource { get; } = new();
        }
    }
}
