using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

    public JobsBuilder WithJob<TJob>() where TJob : class, IJob
    {
        _serviceCollection.TryAddSingleton<TJob>();
        _serviceCollection.AddSingleton<IJob>(sp => sp.GetRequiredService<TJob>());
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
