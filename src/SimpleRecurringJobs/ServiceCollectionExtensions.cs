using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SimpleRecurringJobs.Interval;

namespace SimpleRecurringJobs
{
    public static class ServiceCollectionExtensions
    {
        public static void AddSimpleRecurringJobs(this IServiceCollection services, Action<JobsBuilder> builderFn)
        {
            var builder = new JobsBuilder(services);
            builderFn(builder);

            services.AddSingleton<IJobLogger, MicrosoftLogger>();
            services.AddSingleton<IJobScheduler, IntervalScheduler>();
            services.AddSingleton<IJobExecutor, JobsExecutor>();

            services.AddSingleton(IJobClock.SystemClock);
            services.AddSingleton(IAsyncDelayer.SystemDelayer);

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
}
