using System;
using System.Threading;
using System.Threading.Tasks;
using Cronos;
using Microsoft.Extensions.DependencyInjection;
using SimpleRecurringJobs.Cron;
using SimpleRecurringJobs.InMemory;
using SimpleRecurringJobs.Interval;
using Xunit;
using Xunit.Abstractions;

namespace SimpleRecurringJobs.Tests
{
    public class CronSchedulerTests
    {
        private readonly ITestOutputHelper _output;

        public CronSchedulerTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task TestCronScheduler()
        {
            var sc = new ServiceCollection();

            sc.AddSimpleRecurringJobs(b => b.UseInMemoryJobStore().AddCronScheduler().WithJob<CronJob>());

            var startTime = new DateTime(2020, 04, 10, 07, 00, 00, DateTimeKind.Utc);

            var clock = new MockClock(startTime);
            var delayer = new MockDelayer(clock);
            sc.AddSingleton(_output);
            sc.AddSingleton<IJobClock>(clock);
            sc.AddSingleton<IAsyncDelayer>(delayer);
            sc.AddSingleton<IJobLogger, XunitLogger>();
            await using var sp = sc.BuildServiceProvider();

            var job = sp.GetRequiredService<CronJob>();

            var worker = sp.GetRequiredService<JobsWorker>();
            await worker.StartAsync(new CancellationToken());

            // It should not run immediately
            Assert.Equal(0, job.Counter);

            // It should not run at 07:59
            clock.UtcNow = new DateTime(2020, 04, 10, 07, 59, 00, DateTimeKind.Utc);
            Assert.Equal(0, job.Counter);

            // It should run at 08:00
            clock.UtcNow = new DateTime(2020, 04, 10, 08, 0, 1, DateTimeKind.Utc);
            await Task.Delay(1);
            Assert.Equal(1, job.Counter);

            // It should run again every 24 hours from now.
            for (var i = 2; i < 10; i++)
            {
                clock.UtcNow += TimeSpan.FromDays(1);
                await Task.Delay(100);
                Assert.Equal(i, job.Counter);
            }
        }

        private class CronJob : ICronJob
        {
            public int Counter { get; set; }
            public string Id { get; } = "MockCronJob";

            public Task Execute(CancellationToken cancellationToken)
            {
                Counter++;
                return Task.CompletedTask;
            }
            public CronExpression Cron { get; } = CronExpression.Parse("0 8 * * *"); // Every day 8 am
            public TriggerCountMode Mode { get; } = TriggerCountMode.CountSinceLastTrigger;
        }
    }
}
