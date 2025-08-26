using System;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SimpleRecurringJobs.Interval;

namespace SimpleRecurringJobs;

public static class ServiceCollectionExtensions
{
    public static void AddSimpleRecurringJobs(this IServiceCollection services, Action<JobsBuilder> builderFn)
    {
        services.AddSingleton<IJobLogger, MicrosoftLogger>();
        services.AddSingleton<IJobScheduler, IntervalScheduler>();
        services.AddSingleton<IJobExecutor, JobsExecutor>();
        services.AddSingleton(new JobMetrics(new Meter("SimpleRecurringJobs")));

        services.AddSingleton(IJobClock.SystemClock);
        services.AddSingleton(IAsyncDelayer.SystemDelayer);
        services.AddSingleton(IEnableFeature.AlwaysOn);

        var builder = new JobsBuilder(services);
        builderFn(builder);

        services.AddSingleton(
            sp =>
            {
                var store = sp.GetService<IJobStore>();

                if (store == null)
                    throw new InvalidOperationException("No IJobStore has been registered.");

                return ActivatorUtilities.CreateInstance<JobsWorker>(sp);
            }
        );

        services.AddSingleton<IHostedService>(s => s.GetRequiredService<JobsWorker>());
    }
}
