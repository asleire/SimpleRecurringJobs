using System;
using System.Threading.Tasks;

namespace SimpleRecurringJobs;

/// <summary>
/// Feature for controlling whether SimpleRecurringJobs should be running or not.
/// </summary>
internal interface IEnableFeature
{
    ValueTask<bool> IsEnabled();

    public static IEnableFeature AlwaysOn { get; } = new AlwaysOnFeature();

    private class AlwaysOnFeature : IEnableFeature
    {
        public ValueTask<bool> IsEnabled() => new(true);
    }
}

internal class ConditionalEnableFeature : IEnableFeature
{
    private readonly Func<ValueTask<bool>> _isEnabled;

    public ConditionalEnableFeature(Func<ValueTask<bool>> isEnabled)
    {
        _isEnabled = isEnabled;
    }

    public ValueTask<bool> IsEnabled() => _isEnabled();
}
