using System;

namespace SimpleRecurringJobs.Mongo;

public class JobInfoLock
{
    public string Id { get; set; } = "";

    public string JobInstanceId { get; set; } = "";

    public DateTime LockExpiry { get; set; }
}