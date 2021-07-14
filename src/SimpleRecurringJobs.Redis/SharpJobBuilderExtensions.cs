using System;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace SimpleRecurringJobs.Redis
{
    public static class SimpleRecurringJobsBuilderExtensions
    {
        public static JobsBuilder UseRedisJobStore(
            this JobsBuilder builder,
            Action<SimpleRecurringJobsRedisOptions>? optsFn = null)
        {
            builder.WithJobStore(
                sp =>
                {
                    var connectionMultiplexer = sp.GetService<IConnectionMultiplexer>();

                    if (connectionMultiplexer == null)
                        throw new InvalidOperationException(
                            "No Redis IConnectionMultiplexer is registered. Register one, or use a different method of registered RedisJobStore."
                        );

                    var opts = new SimpleRecurringJobsRedisOptions();
                    optsFn?.Invoke(opts);

                    return ActivatorUtilities.CreateInstance<RedisJobStore>(sp, opts);
                }
            );

            return builder;
        }

        public static JobsBuilder UseRedisJobStore(
            this JobsBuilder builder,
            string redisConnectionString,
            Action<SimpleRecurringJobsRedisOptions>? optsFn = null)
        {
            builder.WithJobStore(
                sp =>
                {
                    var connectionMultiplexer = ConnectionMultiplexer.Connect(redisConnectionString);

                    var opts = new SimpleRecurringJobsRedisOptions();
                    optsFn?.Invoke(opts);

                    return ActivatorUtilities.CreateInstance<RedisJobStore>(sp, connectionMultiplexer, opts);
                }
            );

            return builder;
        }
    }
}