using Microsoft.Extensions.DependencyInjection;

namespace SimpleRecurringJobs.InMemory;

public static class SimpleRecurringJobsBuilderExtensions
{
    public static JobsBuilder UseInMemoryJobStore(this JobsBuilder builder)
    {
        builder.WithJobStore(
            sp => ActivatorUtilities.CreateInstance<InMemoryJobStore>(sp)
        );

        return builder;
    }
}