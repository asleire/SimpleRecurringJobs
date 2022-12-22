using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
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


#if !PRE_NET_5
    [RequiresUnreferencedCode("WithJobsFromAssembly is incompatible with trimming.")]
#endif
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

    public JobsBuilder WithConditionalEnabling(Func<IServiceProvider, Func<ValueTask<bool>>> fn)
    {
        _serviceCollection.AddSingleton<IEnableFeature>(sp =>
        {
            var enabledFn = fn(sp);
            return new ConditionalEnableFeature(enabledFn);
        });
        return this;
    }
}
