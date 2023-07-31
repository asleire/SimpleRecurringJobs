namespace SimpleRecurringJobs.Postgres;

public class SimpleRecurringJobsPostgresOptions
{
    /// <summary>
    /// Database schema to use. Defaults to 'public'
    /// </summary>
    public string Schema { get; set; } = "public";

    /// <summary>
    /// Table to store job data in. Defaults to 'srj_jobs'
    /// </summary>
    public string Table { get; set; } = "srj_jobs";

    /// <summary>
    /// If true, will automatically create the SQL table
    /// </summary>
    public bool AutomaticTableCreation { get; set; } = true;
}
