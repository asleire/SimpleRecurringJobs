using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SimpleRecurringJobs.Mongo;
using Xunit;
using Xunit.Abstractions;

namespace SimpleRecurringJobs.Tests.Mongo;

public class MongoJobStoreTests : StoreTestsBase
{
    private readonly ITestOutputHelper _testOutputHelper;

    public MongoJobStoreTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    protected override async Task<IJobStore> GetStore()
    {
        var sc = new ServiceCollection();

        try
        {
            var connStr = await File.ReadAllTextAsync("mongoconnstr.testcfg");
            connStr = connStr.Trim();
            var testId = Guid.NewGuid().ToString("N");
            sc.AddSimpleRecurringJobs(
                b => b.UseMongoJobStore(
                    connStr,
                    o =>
                    {
                        o.LockCollectionName = $"tests_{testId}_locks";
                        o.JobInfoCollectionName = $"tests_{testId}_jobinfo";
                    }
                )
            );
        }
        catch (Exception)
        {
            _testOutputHelper.WriteLine(
                "Create a file named 'mongoconnstr.testcfg' and place a redis connection string in it."
            );
            throw;
        }

        var sp = sc.BuildServiceProvider();

        var store = sp.GetRequiredService<IJobStore>();

        var mongoStore = Assert.IsType<MongoJobStore>(store);

        return mongoStore;
    }
}
