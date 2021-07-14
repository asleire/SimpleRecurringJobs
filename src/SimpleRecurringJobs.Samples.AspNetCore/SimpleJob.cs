using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SimpleRecurringJobs.Interval;

namespace SimpleRecurringJobs.Samples.AspNetCore
{
    /// <summary>
    /// A simple job that runs every 5 seconds.
    /// Since the Mode iTriggerCountMode"IntervalMode.CountSinceLastTrigger"/> and the duration of the job is 5 seconds, the job will execute continuously without pause.
    /// </summary>
    public class SimpleJob : IIntervalJob
    {
        private readonly ILogger<SimpleJob> _logger;

        public SimpleJob(ILogger<SimpleJob> logger)
        {
            _logger = logger;
        }

        public string Id { get; } = "SimpleJob";

        public async Task  Execute(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"SimpleJob just started. The clock is: {DateTime.UtcNow:T}");
            await Task.Delay(5000, cancellationToken);
            _logger.LogInformation($"SimpleJob just completed. The clock is: {DateTime.UtcNow:T}");
        }

        public TimeSpan Interval { get; } = TimeSpan.FromSeconds(5);
        public TriggerCountMode Mode { get; } = TriggerCountMode.CountSinceLastTrigger;
    }
}
