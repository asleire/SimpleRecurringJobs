using System;
using System.Threading;
using System.Threading.Tasks;
using Cronos;
using Microsoft.Extensions.Logging;
using SimpleRecurringJobs.Cron;

namespace SimpleRecurringJobs.Samples.NetCoreConsole;

public class SimpleCronJob : ICronJob
{
    private readonly ILogger<SimpleCronJob> _logger;

    public SimpleCronJob(ILogger<SimpleCronJob> logger)
    {
        _logger = logger;
    }

    public string Id { get; } = "SimpleCronJob";

    public Task Execute(CancellationToken cancellationToken)
    {
        _logger.LogInformation($"SimpleCronJob just executed. The clock is: {DateTime.UtcNow:T}");
        return Task.CompletedTask;
    }

    public CronExpression Cron { get; } = CronExpression.Parse("0/5 * * * * *", CronFormat.IncludeSeconds); // Every 5 sec
    public TriggerCountMode Mode { get; } = TriggerCountMode.CountSinceLastCompletion;
}