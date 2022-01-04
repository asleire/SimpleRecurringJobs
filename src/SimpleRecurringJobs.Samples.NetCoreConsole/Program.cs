using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using SimpleRecurringJobs.Cron;
using SimpleRecurringJobs.InMemory;

namespace SimpleRecurringJobs.Samples.NetCoreConsole;

class Program
{
    static async Task Main(string[] args)
    {
        using var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices(
                    sc =>
                    {
                        sc.AddSimpleRecurringJobs(
                            b => b.UseInMemoryJobStore().AddCronScheduler().WithJob<SimpleCronJob>()
                        );
                    }
                ).Build()
            ;

        await host.RunAsync();
    }
}