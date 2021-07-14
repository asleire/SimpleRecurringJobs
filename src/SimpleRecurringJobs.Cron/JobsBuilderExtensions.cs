using Microsoft.Extensions.DependencyInjection;

namespace SimpleRecurringJobs.Cron
{
    public static class JobsBuilderExtensions
    {
        public static JobsBuilder AddCronScheduler(this JobsBuilder builder)
        {
            builder.Services.AddSingleton<IJobScheduler, CronScheduler>();
            return builder;
        }
    }
}
