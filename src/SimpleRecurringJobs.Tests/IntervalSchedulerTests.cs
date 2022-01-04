using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SimpleRecurringJobs.InMemory;
using SimpleRecurringJobs.Interval;
using Xunit;
using Xunit.Abstractions;

namespace SimpleRecurringJobs.Tests;

public class IntervalScheduleTests
{
    private readonly ITestOutputHelper _output;

    public IntervalScheduleTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task TestIntervalScheduler()
    {
        var sc = new ServiceCollection();

        sc.AddSimpleRecurringJobs(b => b.UseInMemoryJobStore().WithJob<IntervalJob>());

        var clock = new MockClock();
        var delayer = new MockDelayer(clock);
        sc.AddSingleton(_output);
        sc.AddSingleton<IJobClock>(clock);
        sc.AddSingleton<IAsyncDelayer>(delayer);
        sc.AddSingleton<IJobLogger, XunitLogger>();
        await using var sp = sc.BuildServiceProvider();

        var job = sp.GetRequiredService<IntervalJob>();

        var worker = sp.GetRequiredService<JobsWorker>();
        await worker.StartAsync(new CancellationToken());

        // It should run once immediately
        Assert.Equal(1, job.Counter);

        // It should not have run after 5 minutes
        clock.UtcNow += TimeSpan.FromMinutes(5);
        Assert.Equal(1, job.Counter);

        // It should certainly have run again within 10 more minutes.
        for (var i = 2; i < 100; i++)
        {
            clock.UtcNow += TimeSpan.FromMinutes(10);
            Assert.Equal(i, job.Counter);
        }
    }

    private class IntervalJob : IIntervalJob
    {
        public int Counter { get; set; }
        public string Id { get; } = "MockIntervalJob";

        public Task Execute(CancellationToken cancellationToken)
        {
            Counter++;
            return Task.CompletedTask;
        }

        public TimeSpan Interval { get; } = TimeSpan.FromMinutes(10);
        public TriggerCountMode Mode { get; } = TriggerCountMode.CountSinceLastTrigger;
    }
}