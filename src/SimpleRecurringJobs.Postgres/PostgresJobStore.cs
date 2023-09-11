using Npgsql;

namespace SimpleRecurringJobs.Postgres;

public class PostgresJobsStore : IJobStore
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly SimpleRecurringJobsPostgresOptions _options;
    private readonly string _tableIdentifier;
    private readonly string _pkeyIdentifier;
    private readonly Lazy<Task> _initTask;
    private readonly IJobClock _jobClock;

    public PostgresJobsStore(NpgsqlDataSource dataSource, SimpleRecurringJobsPostgresOptions options,
        IJobClock jobClock)
    {
        _dataSource = dataSource;
        _options = options;
        _jobClock = jobClock;
        _tableIdentifier = $"\"{options.Schema}\".\"{options.Table}\"";
        _pkeyIdentifier = $"\"{options.Table}_pkey\"";
        _initTask = new Lazy<Task>(Initialize);
    }

    private async Task Initialize()
    {
        if (!_options.AutomaticTableCreation)
            return;

        var sql = $"""
CREATE TABLE IF NOT EXISTS {_tableIdentifier}
(
    job_id text COLLATE pg_catalog."default" NOT NULL,
    last_triggered timestamp with time zone NOT NULL,
    last_failure timestamp with time zone NOT NULL,
    last_success timestamp with time zone NOT NULL,
    current_job_instance_id text COLLATE pg_catalog."default",
    current_job_instance_expiry timestamp with time zone,
    CONSTRAINT {_pkeyIdentifier} PRIMARY KEY (job_id)
)
""";

        await using var cmd = _dataSource.CreateCommand(sql);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task EnsureInitialized() => await _initTask.Value;

    public async Task<IJobLock?> TryLock(IJob job, string jobInstanceId)
    {
        await EnsureInitialized();

        var defaultJobInfo = JobInfo.CreateDefaultInfo(job.Id);
        var now = _jobClock.UtcNow;
        var expiry = now + TimeSpan.FromMinutes(10);

        var sql = $"""
INSERT INTO {_tableIdentifier} (job_id, last_triggered,last_failure, last_success, current_job_instance_id, current_job_instance_expiry)
VALUES ($1, $2, $3, $4, $5, $6)
ON CONFLICT ON CONSTRAINT {_pkeyIdentifier}
DO UPDATE SET current_job_instance_id=$5, current_job_instance_expiry=$6 WHERE {_tableIdentifier}.current_job_instance_id is null OR {_tableIdentifier}.current_job_instance_expiry < $7
""";

        await using var cmd = _dataSource.CreateCommand(sql);
        cmd.Parameters.AddRange(new[]
        {
            new NpgsqlParameter { Value = defaultJobInfo.Id },
            new NpgsqlParameter { Value = defaultJobInfo.LastTriggered },
            new NpgsqlParameter { Value = defaultJobInfo.LastFailure },
            new NpgsqlParameter { Value = defaultJobInfo.LastSuccess },
            new NpgsqlParameter { Value = jobInstanceId },
            new NpgsqlParameter { Value = expiry },
            new NpgsqlParameter { Value = now },
        });
        var affectedRows = await cmd.ExecuteNonQueryAsync();

        if (affectedRows != 1)
            return null;

        return new PostgresJobLock(RefreshLock, DisposeLock, jobInstanceId, job);
    }

    public async Task<JobInfo> Retrieve(IJob job)
    {
        await EnsureInitialized();

        var sql = $"""
SELECT last_triggered, last_failure, last_success FROM {_tableIdentifier} WHERE job_id=$1
""";

        await using var cmd = _dataSource.CreateCommand(sql);
        cmd.Parameters.Add(new NpgsqlParameter()
        {
            Value = job.Id
        });
        await using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return JobInfo.CreateDefaultInfo(job.Id);

        return new JobInfo()
        {
            Id = job.Id,
            LastTriggered = reader.GetDateTime(0),
            LastFailure = reader.GetDateTime(1),
            LastSuccess = reader.GetDateTime(2),
        };
    }

    public async Task Save(JobInfo info)
    {
        await EnsureInitialized();

        var sql = $"""
INSERT INTO {_tableIdentifier} (job_id, last_triggered,last_failure, last_success)
VALUES ($1, $2, $3, $4)
ON CONFLICT ON CONSTRAINT {_pkeyIdentifier}
DO UPDATE SET last_triggered=$2, last_failure=$3, last_success=$4
""";

        await using var cmd = _dataSource.CreateCommand(sql);
        cmd.Parameters.AddRange(new[]
        {
            new NpgsqlParameter { Value = info.Id },
            new NpgsqlParameter { Value = info.LastTriggered },
            new NpgsqlParameter { Value = info.LastFailure },
            new NpgsqlParameter { Value = info.LastSuccess },
        });
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task RefreshLock(IJob job, string jobInstanceId)
    {
        var nextExpiry = _jobClock.UtcNow + TimeSpan.FromMinutes(10);
        
        var sql = $"""
UPDATE {_tableIdentifier} SET current_job_instance_expiry=$3 WHERE job_id=$1 AND current_job_instance_id=$2
""";
        await using var cmd = _dataSource.CreateCommand(sql);
        cmd.Parameters.AddRange(new[]
        {
            new NpgsqlParameter { Value = job.Id },
            new NpgsqlParameter { Value = jobInstanceId },
            new NpgsqlParameter { Value = nextExpiry },
        });
        var affectedRows = await cmd.ExecuteNonQueryAsync();

        if (affectedRows != 1)
            throw new SimpleRecurringJobsLockNotValidException();
    }

    private async Task DisposeLock(IJob job, string jobInstanceId)
    {
        var sql = $"""
UPDATE {_tableIdentifier} SET current_job_instance_id=null WHERE job_id=$1 AND current_job_instance_id=$2
""";
        await using var cmd = _dataSource.CreateCommand(sql);
        cmd.Parameters.AddRange(new[]
        {
            new NpgsqlParameter { Value = job.Id },
            new NpgsqlParameter { Value = jobInstanceId },
        });
        await cmd.ExecuteNonQueryAsync();
    }

    class PostgresJobLock : IJobLock
    {
        private readonly Func<IJob, string, Task> _refreshCallback;
        private readonly Func<IJob, string, Task> _disposeCallback;
        private readonly IJob _job;
        private readonly string _jobInstanceId;


        public PostgresJobLock(Func<IJob, string, Task> refreshCallback, Func<IJob, string, Task> disposeCallback, string jobInstanceId, IJob job)
        {
            _refreshCallback = refreshCallback;
            _disposeCallback = disposeCallback;
            _jobInstanceId = jobInstanceId;
            _job = job;
        }

        public async Task Refresh()
        {
            await _refreshCallback(_job, _jobInstanceId);
        }

        public async ValueTask DisposeAsync()
        {
            await _disposeCallback(_job, _jobInstanceId);
        }
    }
}
