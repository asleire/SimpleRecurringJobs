using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleRecurringJobs;

/// <summary>
///     Provides fluent configuration for SimpleRecurringJobs.
/// </summary>
public class JobsBuilder
{
    private readonly IServiceCollection _serviceCollection;

    public JobsBuilder(IServiceCollection serviceCollection)
    {
        _serviceCollection = serviceCollection;
    }

    public IServiceCollection Services => _serviceCollection;

    public JobsBuilder WithJob<TJob>() where TJob : IJob
    {
        _serviceCollection.AddSingleton(typeof(TJob));
        _serviceCollection.AddSingleton(s => (IJob) s.GetRequiredService(typeof(TJob)));
        return this;
    }

    public JobsBuilder WithJobsFromAssembly<TJob>() where TJob : IJob
    {
        foreach (var type in typeof(TJob).Assembly.GetTypes().Where(typeof(IJob).IsAssignableFrom))
            _serviceCollection.AddSingleton(typeof(IJob), type);

        return this;
    }

    public JobsBuilder WithJobStore(Func<IServiceProvider, IJobStore> fn)
    {
        _serviceCollection.AddSingleton(fn);
        return this;
    }
}