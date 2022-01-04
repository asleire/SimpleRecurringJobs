namespace SimpleRecurringJobs;

public enum TriggerCountMode
{
    /// <summary>
    ///     Will use the job's last trigger timestamp to determine when the job should next be executed.
    /// </summary>
    CountSinceLastTrigger = 0,

    /// <summary>
    ///     Will use the job's last completion timestamp to determine when the job should next be executed.
    /// </summary>
    CountSinceLastCompletion = 1
}