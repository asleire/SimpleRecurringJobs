using System;

namespace SimpleRecurringJobs;

/// <summary>
///     Base exception for exceptions from this library.
/// </summary>
public class SimpleRecurringJobsException : Exception
{
    public SimpleRecurringJobsException(string message)
        : base(message)
    {
    }
}

public class SimpleRecurringJobsJobNotSchedulableException : SimpleRecurringJobsException
{
    public SimpleRecurringJobsJobNotSchedulableException(IJob job)
        : base($"No registered schedulers are able to process job '{job.Id}'.")
    {
        JobId = job.Id;
    }

    public string JobId { get; }
}

public class SimpleRecurringJobsJobMultipleSchedulersException : SimpleRecurringJobsException
{
    public SimpleRecurringJobsJobMultipleSchedulersException(IJob job)
        : base($"The job '{job.Id}' should only be handled by a single scheduler, but multiple were found.")
    {
    }
}

public class SimpleRecurringJobsLockNotValidException : SimpleRecurringJobsException
{
    public SimpleRecurringJobsLockNotValidException()
        : base(
            "The Job Lock is no longer valid. It may have expired, or someone may have manually removed it from the store."
        )
    {
    }
}

public class SimpleRecurringJobsDuplicateJobIdException : SimpleRecurringJobsException
{
    public SimpleRecurringJobsDuplicateJobIdException(IJob job)
        : base($"There are multiple registrations of a job with id '{job.Id}'")
    {
    }
}