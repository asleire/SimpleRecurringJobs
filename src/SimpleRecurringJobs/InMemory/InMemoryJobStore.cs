using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleRecurringJobs.InMemory;

public class InMemoryJobStore : IJobStore
{
    private readonly IJobClock _clock;
    private readonly Dictionary<string, JobInfo> _info = new();
    private readonly object _lock = new();
    private readonly Dictionary<string, InMemoryLock> _locks = new();

    public InMemoryJobStore(IJobClock clock)
    {
        _clock = clock;
    }

    public Task<IJobLock?> TryLock(IJob job, string jobInstanceId)
    {
        lock (_lock)
        {
            if (_locks.ContainsKey(job.Id))
                return Task.FromResult<IJobLock?>(null);
            var newLock = _locks[job.Id] = new InMemoryLock(
                this,
                job.Id,
                jobInstanceId,
                _clock.UtcNow + TimeSpan.FromMinutes(10)
            );
            return Task.FromResult<IJobLock?>(newLock);
        }
    }

    public Task<JobInfo> Retrieve(IJob job)
    {
        if (!_info.TryGetValue(job.Id, out var result))
            result = JobInfo.CreateDefaultInfo(job.Id);

        return Task.FromResult(result);
    }

    public Task Save(JobInfo info)
    {
        _info[info.Id] = info;
        return Task.CompletedTask;
    }

    private class InMemoryLock : IJobLock
    {
        private readonly string _instanceId;
        private readonly string _key;
        private readonly InMemoryJobStore _store;

        public DateTime Expiration;

        public InMemoryLock(InMemoryJobStore store, string key, string instanceId, DateTime expiration)
        {
            _store = store;
            _key = key;
            _instanceId = instanceId;
            Expiration = expiration;
        }

        public Task Refresh()
        {
            lock (_store._lock)
            {
                if (!_store._locks.TryGetValue(_key, out var @lock) || @lock._instanceId != _instanceId)
                    throw new SimpleRecurringJobsLockNotValidException();
                Expiration = _store._clock.UtcNow + TimeSpan.FromMinutes(10);
            }

            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            lock (_store._lock)
            {
                if (!_store._locks.TryGetValue(_key, out var @lock) || @lock._instanceId != _instanceId)
                    throw new SimpleRecurringJobsLockNotValidException();
                _store._locks.Remove(_key);
            }

            return new ValueTask();
        }
    }
}