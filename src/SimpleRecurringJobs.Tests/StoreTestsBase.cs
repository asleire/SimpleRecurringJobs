using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SimpleRecurringJobs.Tests
{
    public abstract class StoreTestsBase
    {
        protected abstract Task<IJobStore> GetStore();


        [Fact]
        public async Task NewDataShouldBeBlank()
        {
            var store = await GetStore();

            var job = new StoreTestJob();

            var info = await store.Retrieve(job);

            Assert.Equal(DateTime.MinValue, info.LastFailure, TimeSpan.FromMilliseconds(1));
            Assert.Equal(DateTime.MinValue, info.LastTriggered, TimeSpan.FromMilliseconds(1));
            Assert.Equal(DateTime.MinValue, info.LastSuccess, TimeSpan.FromMilliseconds(1));
        }


        [Fact]
        public async Task SaveAndRetrieve()
        {
            var store = await GetStore();

            var job = new StoreTestJob();

            var failure = DateTime.UtcNow - TimeSpan.FromMinutes(30);
            var triggered = DateTime.UtcNow - TimeSpan.FromMinutes(20);
            var success = DateTime.UtcNow;

            await store.Save(
                new JobInfo
                {
                    Id = job.Id,
                    LastFailure = failure,
                    LastSuccess = success,
                    LastTriggered = triggered
                }
            );

            var info = await store.Retrieve(job);

            Assert.Equal(failure, info.LastFailure, TimeSpan.FromMilliseconds(1));
            Assert.Equal(triggered, info.LastTriggered, TimeSpan.FromMilliseconds(1));
            Assert.Equal(success, info.LastSuccess, TimeSpan.FromMilliseconds(1));
        }

        [Fact]
        public async Task MultipleLocksSameJobOnlyOneShouldSucceed()
        {
            var store = await GetStore();

            var job = new StoreTestJob();

            var lockTasks = new List<Task<IJobLock?>>();

            for (var i = 0; i < 100; i++)
                lockTasks.Add(store.TryLock(job, Guid.NewGuid().ToString()));

            var locks = await Task.WhenAll(lockTasks);

            Assert.Single(locks, l => l != null);
        }

        [Fact]
        public async Task MultipleLocksDifferentJobsAllShouldSucceed()
        {
            var store = await GetStore();

            var lockTasks = new List<Task<IJobLock?>>();

            for (var i = 0; i < 100; i++)
            {
                var job = new StoreTestJob
                {
                    Id = Guid.NewGuid().ToString()
                };
                lockTasks.Add(store.TryLock(job, Guid.NewGuid().ToString()));
            }

            var locks = await Task.WhenAll(lockTasks);

            Assert.All(locks, Assert.NotNull);
        }


        protected class StoreTestJob : IJob
        {
            public string Id { get; set; } = "myjob";

            public Task Execute(CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }
    }
}
