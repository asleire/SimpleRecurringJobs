using Microsoft.Extensions.DependencyInjection;
using SimpleRecurringJobs.Postgres;
using Xunit;
using Xunit.Abstractions;

namespace SimpleRecurringJobs.Tests.Postgres;

public class PostgresJobStoreTests : StoreTestsBase
{
    private readonly ITestOutputHelper _testOutputHelper;

    public PostgresJobStoreTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    protected override async Task<IJobStore> GetStore()
    {
        var sc = new ServiceCollection();

        try
        {
            var connStr = (await File.ReadAllTextAsync("postgresconnstr.testcfg")).Trim();
            sc.AddSimpleRecurringJobs(
                b => b.UsePostgresJobStore(connStr, o => o.Table = $"sjt_{Guid.NewGuid()}")
            );
        }
        catch (Exception)
        {
            _testOutputHelper.WriteLine(
                "Create a file named 'postgresconnstr.testcfg' and place a postgresql connection string in it."
            );
            throw;
        }

        var sp = sc.BuildServiceProvider();

        var store = sp.GetRequiredService<IJobStore>();

        var typedStore = Assert.IsType<PostgresJobsStore>(store);

        return typedStore;
    }
}
