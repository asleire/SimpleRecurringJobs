using System;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace SimpleRecurringJobs.Postgres;

public static class SimpleRecurringJobsBuilderExtensions
{
    /// <summary>
    /// Configures MongoDB as a JobStore. Retrieves <see cref="NpgDataSource"/> from <see cref="IServiceProvider"/>
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="optsFn"></param>
    /// <returns></returns>
    public static JobsBuilder UsePostgresJobStore(
        this JobsBuilder builder,
        Action<SimpleRecurringJobsPostgresOptions>? optsFn = null)
    {
        return UsePostgresJobStore(builder, sp => sp.GetRequiredService<NpgsqlDataSource>(), optsFn);
    }
    
    public static JobsBuilder UsePostgresJobStore(
        this JobsBuilder builder,
        Func<IServiceProvider, NpgsqlDataSource> dbResolver,
        Action<SimpleRecurringJobsPostgresOptions>? optsFn = null)
    {
        builder.WithJobStore(
            sp =>
            {
                var db = dbResolver(sp);
                    
                var opts = new SimpleRecurringJobsPostgresOptions();
                optsFn?.Invoke(opts);

                return ActivatorUtilities.CreateInstance<PostgresJobsStore>(sp, db, opts);
            }
        );

        return builder;
    }

    public static JobsBuilder UsePostgresJobStore(
        this JobsBuilder builder,
        string postgresConnectionString,
        Action<SimpleRecurringJobsPostgresOptions>? optsFn = null)
    {
        builder.WithJobStore(
            sp =>
            {
                var opts = new SimpleRecurringJobsPostgresOptions();
                optsFn?.Invoke(opts);

                var npgsqlDataSource = NpgsqlDataSource.Create(postgresConnectionString);
                

                return ActivatorUtilities.CreateInstance<PostgresJobsStore>(sp, npgsqlDataSource, opts);
            }
        );

        return builder;
    }
}
