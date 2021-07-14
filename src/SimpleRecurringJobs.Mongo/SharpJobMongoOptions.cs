namespace SimpleRecurringJobs.Mongo
{
    public class SimpleRecurringJobsMongoOptions
    {
        public string JobInfoCollectionName { get; set; } = "SimpleRecurringJobs:JobInfo";

        public string LockCollectionName { get; set; } = "SimpleRecurringJobs:JobLock";

        /// <summary>
        ///     Name of the Mongo Database to put the collections in.
        /// </summary>
        public string Database { get; set; } = "";
    }
}