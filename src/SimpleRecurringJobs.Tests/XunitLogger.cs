using System;
using System.Diagnostics.Tracing;
using Xunit.Abstractions;

namespace SimpleRecurringJobs.Tests;

public class XunitLogger : IJobLogger
{
    private readonly ITestOutputHelper _outputHelper;

    public XunitLogger(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    public void Log(EventLevel level, Exception? ex, string message, params object[] parameters)
    {
        _outputHelper.WriteLine($"{level}: {message}");
    }

    public IJobLogger ForSource<T>()
    {
        return this;
    }
}