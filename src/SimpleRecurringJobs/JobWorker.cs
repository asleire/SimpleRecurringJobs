using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace SimpleRecurringJobs
{
    internal class JobsWorker : BackgroundService
    {
        private readonly IAsyncDelayer _delayer;
        private readonly IJobExecutor _jobExecutor;
        private readonly IEnumerable<IJob> _jobs;
        private readonly IJobStore _jobStore;
        private readonly IJobLogger _logger;
        private readonly IEnumerable<IJobScheduler> _schedulers;

        public JobsWorker(
            IEnumerable<IJob> jobs,
            IEnumerable<IJobScheduler> schedulers,
            IJobLogger logger,
            IJobStore jobStore,
            IAsyncDelayer delayer,
            IJobExecutor jobExecutor)
        {
            _jobs = jobs.ToList();
            _schedulers = schedulers;
            _jobStore = jobStore;
            _delayer = delayer;
            _jobExecutor = jobExecutor;
            _logger = logger.ForSource<JobsWorker>();
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

            var tasks = new List<Task>();

            foreach (var job in jobs) tasks.Add(RunJob(job, schedulerMap[job.Id], cancellationToken));

            await Task.WhenAll(tasks);
        }

        private async Task RunJob(IJob job, IJobScheduler scheduler, CancellationToken cancellationToken)
        {
            var errorCount = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                JobInfo info;

                try
                {
                    info = await _jobStore.Retrieve(job);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Unexpected internal error occurred retrieving job info for job {JobId}.",
                        job.Id
                    );
                    errorCount++;
                    if (errorCount > 10) throw;

                    await _delayer.Delay(1000 * errorCount, cancellationToken);
                    continue;
                }

                try
                {
                    var shouldExecuteNow = await scheduler.WaitUntilNext(job, info, cancellationToken);

                    if (!shouldExecuteNow)
                        continue;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(
                        ex,
                        "Unexpected internal error while waiting for next trigger for job {JobId}.",
                        job.Id
                    );
                    errorCount++;
                    if (errorCount > 10) throw;

                    await _delayer.Delay(1000 * errorCount, cancellationToken);
                    continue;
                }

                try
                {
                    var success = await _jobExecutor.Execute(job, cancellationToken);

                    if (!success)
                        // A slight pause to ensure we don't spam-loop in case of errors or existing lock.
                        await Task.Delay(1000, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(
                        ex,
                        "Unexpected internal error occurred during execution of job {JobId}.",
                        job.Id
                    );
                }
            }
        }
    }
}