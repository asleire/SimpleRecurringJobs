using System;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace SimpleRecurringJobs.Mongo;

public class MongoJobStore : IJobStore
{
    private readonly Task<IMongoCollection<JobInfo>> _infoCollection;
    private readonly Task<IMongoCollection<JobInfoLock>> _lockCollection;
    private readonly SimpleRecurringJobsMongoOptions _options;

    public MongoJobStore(IMongoDatabase db, SimpleRecurringJobsMongoOptions options)
    {
        _options = options;
        _infoCollection = InitializeJobInfoCollection(db);
        _lockCollection = InitializeJobInfoLockCollection(db);
    }

    public async Task<JobInfo> Retrieve(IJob job)
    {
        var coll = await _infoCollection;

        var result = await coll.Find(j => j.Id == job.Id).FirstOrDefaultAsync();

        return result ?? JobInfo.CreateDefaultInfo(job.Id);
    }

    public async Task<IJobLock?> TryLock(IJob job, string jobInstanceId)
    {
        var coll = await _lockCollection;

        var existing = await coll.Find(j => j.Id == job.Id).FirstOrDefaultAsync();

        if (existing != null)
            return null;

        try
        {
            var toInsert = new JobInfoLock
            {
                Id = job.Id,
                JobInstanceId = jobInstanceId,
                LockExpiry = DateTime.UtcNow + TimeSpan.FromMinutes(10)
            };
            await coll.InsertOneAsync(toInsert);
            return new MongoJobLock(coll, job.Id, jobInstanceId);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return null;
        }
    }

    public async Task Save(JobInfo info)
    {
        var coll = await _infoCollection;

        await coll.FindOneAndReplaceAsync<JobInfo>(
            j => j.Id == info.Id,
            info,
            new FindOneAndReplaceOptions<JobInfo> {IsUpsert = true}
        );
    }

    private Task<IMongoCollection<JobInfo>> InitializeJobInfoCollection(IMongoDatabase db)
    {
        BsonClassMapUtil.RegisterClassMap<JobInfo>(
            map =>
            {
                map.AutoMap();
                map.SetIgnoreExtraElements(true);
            }
        );

        var coll = db.GetCollection<JobInfo>(_options.JobInfoCollectionName);

        return Task.FromResult(coll);
    }

    private async Task<IMongoCollection<JobInfoLock>> InitializeJobInfoLockCollection(IMongoDatabase db)
    {
        BsonClassMapUtil.RegisterClassMap<JobInfoLock>(
            map =>
            {
                map.AutoMap();
                map.SetIgnoreExtraElements(true);
            }
        );

        var coll = db.GetCollection<JobInfoLock>(_options.LockCollectionName);

        await coll.Indexes.CreateOneAsync(
            new CreateIndexModel<JobInfoLock>(
                Builders<JobInfoLock>.IndexKeys.Ascending(j => j.LockExpiry),
                new CreateIndexOptions {ExpireAfter = TimeSpan.FromMinutes(10)}
            )
        );

        return coll;
    }

    private class MongoJobLock : IJobLock
    {
        private readonly IMongoCollection<JobInfoLock> _collection;
        private readonly string _jobId;
        private readonly string _jobInstanceId;

        public MongoJobLock(IMongoCollection<JobInfoLock> collection, string jobId, string jobInstanceId)
        {
            _collection = collection;
            _jobId = jobId;
            _jobInstanceId = jobInstanceId;
        }

        public async Task Refresh()
        {
            var updateDef = Builders<JobInfoLock>.Update.Set(
                j => j.LockExpiry,
                DateTime.UtcNow + TimeSpan.FromMinutes(10)
            );
            var result = await _collection.FindOneAndUpdateAsync(
                j => j.Id == _jobId && j.JobInstanceId == _jobInstanceId,
                updateDef
            );

            if (result == null)
                throw new SimpleRecurringJobsLockNotValidException();
        }

        public async ValueTask DisposeAsync()
        {
            await _collection.FindOneAndDeleteAsync(j => j.Id == _jobId && j.JobInstanceId == _jobInstanceId);
        }
    }
}