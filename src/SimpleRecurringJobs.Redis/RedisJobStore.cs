using System;
using System.Text.Json;
using System.Threading.Tasks;
using StackExchange.Redis;
using StackExchange.Redis.KeyspaceIsolation;

namespace SimpleRecurringJobs.Redis;

internal class RedisJobStore : IJobStore
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly SimpleRecurringJobsRedisOptions _simpleRecurringJobsRedisOptions;


    public RedisJobStore(
        IConnectionMultiplexer connectionMultiplexer,
        SimpleRecurringJobsRedisOptions simpleRecurringJobsRedisOptions)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _simpleRecurringJobsRedisOptions = simpleRecurringJobsRedisOptions;
    }

    private IDatabase Db => _connectionMultiplexer.GetDatabase()
        .WithKeyPrefix(_simpleRecurringJobsRedisOptions.KeyPrefix);

    public async Task<JobInfo> Retrieve(IJob job)
    {
        var redisValue = await Db.StringGetAsync($"SimpleRecurringJobs:{job.Id}");

        if (string.IsNullOrEmpty(redisValue))
            return JobInfo.CreateDefaultInfo(job.Id);

        return JsonSerializer.Deserialize<JobInfo>(redisValue) ?? JobInfo.CreateDefaultInfo(job.Id);
    }

    public async Task<IJobLock?> TryLock(IJob job, string jobInstanceId)
    {
        var lockKey = $"SimpleRecurringJobs:{job.Id}:Lock";
        var hasLock = await Db.LockTakeAsync(
            lockKey,
            jobInstanceId,
            TimeSpan.FromMinutes(10)
        );

        if (!hasLock)
            return null;

        return new RedisLock(() => Db, lockKey, jobInstanceId);
    }

    public async Task Save(JobInfo info)
    {
        var serializedValue = JsonSerializer.Serialize(info);

        await Db.StringSetAsync($"SimpleRecurringJobs:{info.Id}", serializedValue);
    }

    private class RedisLock : IJobLock
    {
        private readonly Func<IDatabase> _dbFunc;
        private readonly string _key;
        private readonly string _value;

        public RedisLock(Func<IDatabase> dbFunc, string key, string value)
        {
            _dbFunc = dbFunc;
            _key = key;
            _value = value;
        }

        public async Task Refresh()
        {
            await _dbFunc().LockExtendAsync(_key, _value, TimeSpan.FromMinutes(10));
        }

        public async ValueTask DisposeAsync()
        {
            await _dbFunc().LockReleaseAsync(_key, _value);
        }
    }
}