using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SimpleRecurringJobs.Redis;
using Xunit;
using Xunit.Abstractions;

namespace SimpleRecurringJobs.Tests.Redis
{
    public class RedisJobStoreTests : StoreTestsBase
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public RedisJobStoreTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        protected override async Task<IJobStore> GetStore()
        {
            var sc = new ServiceCollection();

            try
            {
                var connStr = await File.ReadAllTextAsync("redisconnstr.testcfg");
                sc.AddSimpleRecurringJobs(
                    b => b.UseRedisJobStore(connStr, o => o.KeyPrefix = $"SharpJobTests:{Guid.NewGuid()}:")
                );
            }
            catch (Exception)
            {
                _testOutputHelper.WriteLine(
                    "Create a file named 'redisconnstr.testcfg' and place a redis connection string in it."
                );
                throw;
            }

            var sp = sc.BuildServiceProvider();

            var store = sp.GetRequiredService<IJobStore>();

            var redisStore = Assert.IsType<RedisJobStore>(store);

            return redisStore;
        }
    }
}
