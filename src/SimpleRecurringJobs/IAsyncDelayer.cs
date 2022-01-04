using System;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleRecurringJobs;

/// <summary>
///     An interface to wrap Task.Delay, allowing us to mock it.
/// </summary>
public interface IAsyncDelayer
{
    public static IAsyncDelayer SystemDelayer { get; } = new SystemAsyncDelayer();
    Task Delay(TimeSpan timeSpan, CancellationToken cancellationToken);

    public async Task Delay(int milliseconds, CancellationToken cancellationToken)
    {
        await Delay(TimeSpan.FromMilliseconds(milliseconds), cancellationToken);
    }

    private class SystemAsyncDelayer : IAsyncDelayer
    {
        public async Task Delay(TimeSpan timeSpan, CancellationToken cancellationToken)
        {
            await Task.Delay(timeSpan, cancellationToken);
        }
    }
}
