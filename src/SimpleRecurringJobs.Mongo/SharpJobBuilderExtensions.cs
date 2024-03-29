﻿using System;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;

namespace SimpleRecurringJobs.Mongo;

public static class SimpleRecurringJobsBuilderExtensions
{
    /// <summary>
    /// Configures MongoDB as a JobStore. Retrieves <see cref="IMongoDatabase"/> from <see cref="IServiceProvider"/>
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="optsFn"></param>
    /// <returns></returns>
    public static JobsBuilder UseMongoJobStore(
        this JobsBuilder builder,
        Action<SimpleRecurringJobsMongoOptions>? optsFn = null)
    {
        return UseMongoJobStore(builder, sp => sp.GetRequiredService<IMongoDatabase>(), optsFn);
    }
    
    public static JobsBuilder UseMongoJobStore(
        this JobsBuilder builder,
        Func<IServiceProvider, IMongoDatabase> dbResolver,
        Action<SimpleRecurringJobsMongoOptions>? optsFn = null)
    {
        builder.WithJobStore(
            sp =>
            {
                var opts = new SimpleRecurringJobsMongoOptions();
                optsFn?.Invoke(opts);

                var db = dbResolver(sp);

                return ActivatorUtilities.CreateInstance<MongoJobStore>(sp, db, opts);
            }
        );

        return builder;
    }

    public static JobsBuilder UseMongoJobStore(
        this JobsBuilder builder,
        string mongoConnectionString,
        Action<SimpleRecurringJobsMongoOptions>? optsFn = null)
    {
        builder.WithJobStore(
            sp =>
            {
                var opts = new SimpleRecurringJobsMongoOptions();
                optsFn?.Invoke(opts);

                var dbName = opts.Database;

                if (string.IsNullOrEmpty(dbName))
                {
                    var connString = new ConnectionString(mongoConnectionString);

                    if (string.IsNullOrEmpty(connString.DatabaseName))
                        throw new Exception("Database must be specified in Mongo Connection string or in options.");

                    dbName = connString.DatabaseName;
                }

                var mongoClient = new MongoClient(mongoConnectionString);
                var db = mongoClient.GetDatabase(dbName);

                return ActivatorUtilities.CreateInstance<MongoJobStore>(sp, db, opts);
            }
        );

        return builder;
    }
}
