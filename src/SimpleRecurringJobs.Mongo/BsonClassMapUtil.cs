using System;
using System.Collections.Concurrent;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace SimpleRecurringJobs.Mongo
{
    internal static class BsonClassMapUtil
    {
        private static readonly ConcurrentDictionary<Type, object> LockMap = new();

        static BsonClassMapUtil()
        {
            var pack = new ConventionPack
            {
                new IgnoreExtraElementsConvention(true)
            };
            ConventionRegistry.Register("engine-conventions", pack, t => true);
        }

        /// <summary>
        ///     Helper method to prevent concurrency errors in integration tests.
        /// </summary>
        public static void RegisterClassMap<T>(Action<BsonClassMap<T>> mapFn)
        {
            var @lock = LockMap.GetOrAdd(typeof(T), new object());
            lock (@lock)
            {
                if (BsonClassMap.IsClassMapRegistered(typeof(T)))
                    return;
                BsonClassMap.RegisterClassMap(mapFn);
            }
        }
    }
}