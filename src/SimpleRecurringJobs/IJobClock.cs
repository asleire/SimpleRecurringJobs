using System;

namespace SimpleRecurringJobs
{
    public interface IJobClock
    {
        DateTime UtcNow { get; }

        public static IJobClock SystemClock { get; } = new SystemJobClock();

        private class SystemJobClock : IJobClock
        {
            public DateTime UtcNow => DateTime.UtcNow;
        }
    }
}
