using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace SimpleRecurringJobs;

internal class JobsWorker : BackgroundService
{
    private readonly IAsyncDelayer _delayer;
    private readonly IJobExecutor _jobExecutor;
    private readonly IEnumerable<IJob> _jobs;
    private readonly IJobStore _jobStore;
    private readonly IJobLogger _logger;
    private readonly IEnumerable<IJobScheduler> _schedulers;
#if PRE_NET_6
    private readonly IHostLifetime _hostLifetime;
#endif

    public JobsWorker(
        IEnumerable<IJob> jobs,
        IEnumerable<IJobScheduler> schedulers,
        IJobLogger logger,
        IJobStore jobStore,
        IAsyncDelayer delayer,
        IJobExecutor jobExecutor
#if PRE_NET_6
        ,
        IHostLifetime hostLifetime
#endif
    )
    {
        _jobs = jobs.ToList();
        _schedulers = schedulers;
        _jobStore = jobStore;
        _delayer = delayer;
        _jobExecutor = jobExecutor;
        _logger = logger.ForSource<JobsWorker>();

#if PRE_NET_6
        _hostLifetime = hostLifetime;
#endif
    }

    /// <summary>
    ///     Assigns each job to a scheduler and runs all schedulers until cancelled.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="SimpleRecurringJobsJobNotSchedulableException"></exception>
    /// <exception cref="SimpleRecurringJobsJobMultipleSchedulersException"></exception>
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInfo("Initializing SimpleRecurringJobs...");
            var jobs = _jobs.ToList();
            var schedulerMap = new Dictionary<string, IJobScheduler>();

            foreach (var job in jobs)
            {
                if (schedulerMap.ContainsKey(job.Id))
                    throw new SimpleRecurringJobsDuplicateJobIdException(job);

                foreach (var scheduler in _schedulers)
                {
                    if (!scheduler.CanHandle(job))
                        continue;

                    if (schedulerMap.ContainsKey(job.Id))
                        throw new SimpleRecurringJobsJobMultipleSchedulersException(job);

                    schedulerMap[job.Id] = scheduler;
                }

                if (!schedulerMap.ContainsKey(job.Id))
                    throw new SimpleRecurringJobsJobNotSchedulableException(job);
            }

            if (jobs.Count == 0)
            {
                _logger.LogInfo("SimpleRecurringJobs found no registered jobs. Stopping.");
                return;
            }

            var tasks = new List<Task>();

            foreach (var job in jobs)
            {
                _logger.LogInfo("Starting job {JobId}...", job.Id);
                tasks.Add(RunJob(job, schedulerMap[job.Id], cancellationToken));
            }

            _logger.LogInfo("SimpleRecurringJobs is running.");
            await Task.WhenAll(tasks);
            _logger.LogInfo("SimpleRecurringJobs was stopped.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "An exception occured while starting SimpleRecurringJobs");

#if PRE_NET_6
            await _hostLifetime.StopAsync(CancellationToken.None);
#endif
            throw;
        }
    }

    private async Task RunJob(IJob job, IJobScheduler scheduler, CancellationToken cancellationToken)
    {
        var exceptionCount = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var action = "";

            try
            {
                action = "Retrieve job info from job store";
                var info = await _jobStore.Retrieve(job);
                
                action = "Wait for next trigger";
                var shouldExecuteNow = await scheduler.WaitUntilNext(job, info, cancellationToken);

                if (!shouldExecuteNow)
                    continue;
                
                action = "Execute job";
                var success = await _jobExecutor.Execute(job, cancellationToken);

                if (!success)
                {
                    // A slight pause to ensure we don't spam-loop in case of errors or existing lock.
                    await Task.Delay(1000, cancellationToken);
                }
                
                exceptionCount = 0;
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(
                    ex,
                    "Unexpected internal error while running job {JobId}. Current action: {Action}",
                    job.Id,
                    action
                );
                exceptionCount++;

                var retryDelay = Math.Min(1000 * exceptionCount, 60_000);

                await _delayer.Delay(retryDelay, cancellationToken);
            }
        }
    }
}