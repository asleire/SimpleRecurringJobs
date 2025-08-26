using System.Diagnostics.Metrics;

namespace SimpleRecurringJobs;

internal class JobMetrics(Meter meter)
{
    public Counter<long> JobStarted { get; } = meter.CreateCounter<long>("srj_job_started", description: "Increments once each time a job is started.");
    public Counter<long> JobCompleted { get; } = meter.CreateCounter<long>("srj_job_completed", description: "Increments once each time a job completes, either successfully or with an error.");
    public Counter<long> JobDurationInMs { get; } = meter.CreateCounter<long>("srj_job_duration_in_ms", description: "Counts the total time spent in a job.");
    
    public Counter<long> JobLockAttemptStarted { get; } = meter.CreateCounter<long>("srj_job_lock_attempt_started", description: "Increments once each time a job lock is attempted to be acquired.");
    
    public Counter<long> JobLockAttemptCompleted { get; } = meter.CreateCounter<long>("srj_job_lock_attempt_completed", description: "Increments once each time a job lock is either acquired or not.");
    
    public Counter<long> JobLockAttemptDurationInMs { get; } = meter.CreateCounter<long>("srj_job_lock_attempt_duration_in_ms", description: "Counts the total time spent acquiring job locks.");
}
