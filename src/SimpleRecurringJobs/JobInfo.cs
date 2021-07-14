using System;

namespace SimpleRecurringJobs
{
    public class JobInfo
    {
        /// <summary>
        ///     ID of the Job.
        /// </summary>
        public string Id { get; set; } = "";

        /// <summary>
        ///     Timestamp of when the job was last triggered.
        /// </summary>
        public DateTime LastTriggered { get; set; }

        /// <summary>
        ///     Timestamp of when the job last failed.
        /// </summary>
        public DateTime LastFailure { get; set; }

        /// <summary>
        ///     Timestamp of when the job last completed without errors.
        /// </summary>
        public DateTime LastSuccess { get; set; }

        public static JobInfo CreateDefaultInfo(string id)
        {
            return new()
            {
                Id = id
            };
        }
    }
}