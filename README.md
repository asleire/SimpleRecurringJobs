# SimpleRecurringJobs
[![NuGet version (SimpleRecurringJobs)](https://img.shields.io/nuget/v/SimpleRecurringJobs.svg?style=flat-square)](https://www.nuget.org/packages/SimpleRecurringJobs/)

SimpleRecurringJobs is a C# library designed for very easily running recurring jobs in your applications, where each job should run once per trigger regardless of how many instances of your application is running. 

## Dependencies
SimpleRecurringJobs is designed for work with .NET Core apps, and make use of the following Microsoft packages:
* `Microsoft.Extensions.DependencyInjection.Abstractions`
* `Microsoft.Extensions.Hosting.Abstractions`
* `Microsoft.Extensions.Logging.Abstractions`

Additional packages are required depending on what extra functionality you need.

## Samples
See [SimpleJob](https://github.com/asleire/SimpleRecurringJobs/blob/main/src/SimpleRecurringJobs.Samples.AspNetCore/SimpleJob.cs) for a simple job that executes with a given interval.

## Jobs
### IIntervalJob
Interval jobs run at certain intervals, disregarding the clock. Suitable for jobs that do not need to be run at specific times.
```
public class SimpleJob : IIntervalJob
{
    public string Id { get; } = "SimpleJob";
    public TimeSpan Interval { get; } = TimeSpan.FromSeconds(5);
    public TriggerCountMode Mode { get; } = TriggerCountMode.CountSinceLastTrigger;

    public async Task Execute(CancellationToken cancellationToken)
    {
        // todo: Do stuff!
    }
}
```

### ICronJob
[![NuGet version (SimpleRecurringJobs.Cron)](https://img.shields.io/nuget/v/SimpleRecurringJobs.Cron.svg?style=flat-square)](https://www.nuget.org/packages/SimpleRecurringJobs.Cron/)

Cron jobs run according to a cron expression. This requires the package `SimpleRecurringJobs.Cron` which has a dependency on the third-party package `Cronos`.

`.AddCronScheduler()` must be called in the builder callback in `AddSimpleRecurringJobs(...)`. 
E.g.: 
```
sc.AddSimpleRecurringJobs(
  b => b.UseInMemoryJobStore().AddCronScheduler().WithJob<SimpleCronJob>()
);
```

```
public class SimpleCronJob : ICronJob
{
    public string Id { get; } = "SimpleCronJob";

    public async Task Execute(CancellationToken cancellationToken)
    {
        _logger.LogInformation($"SimpleCronJob just executed. The clock is: {DateTime.UtcNow:T}");
    }

    public string Cron { get; } = "0/5 * * * * *"; // Every 5 sec
    
    public TriggerCountMode Mode { get; } = TriggerCountMode.CountSinceLastCompletion;
}
```

## Stores
SimpleRecurringJobs requires a data store for storing job timestamps and job locks.

### InMemory
The InMemory job store should only be used for local development. It will not work for multiple instances of your application.
```
services.AddSimpleRecurringJobs(b => b.UseInMemoryJobStore().WithJob<SimpleJob>());
```

## Redis 
[![NuGet version (SimpleRecurringJobs.Redis)](https://img.shields.io/nuget/v/SimpleRecurringJobs.Redis.svg?style=flat-square)](https://www.nuget.org/packages/SimpleRecurringJobs.Redis/)

The package `SimpleRecurringJobs.Redis` allows using Redis for persisting job data. It depends on the Nuget package `StackExchange.Redis`.
```
var redisConnStr = ...; // Get Redis connection string from somewhere.
services.AddSimpleRecurringJobs(b => b.UseRedisJobStore(connStr, o => o.KeyPrefix = "MyJobs:").WithJob<SimpleJob>());
```


## MongoDB
[![NuGet version (SimpleRecurringJobs.Mongo)](https://img.shields.io/nuget/v/SimpleRecurringJobs.Mongo.svg?style=flat-square)](https://www.nuget.org/packages/SimpleRecurringJobs.Mongo/)

The package `SimpleRecurringJobs.Mongo` allows using MongoDB for persisting job data. It depends on the Nuget package `MongoDB.Driver`.
```
var mongoConnStr = ...; // Get MongoDB connection string from somewhere.
services.AddSimpleRecurringJobs(b => b.UseMongoJobStore(mongoConnStr, o => o.Database = "MyAppDb").WithJob<SimpleJob>());
```
