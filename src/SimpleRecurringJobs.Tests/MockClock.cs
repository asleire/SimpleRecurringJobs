using System;

namespace SimpleRecurringJobs.Tests;

public class MockClock : IJobClock
{
    private DateTime _utcNow = DateTime.UtcNow;

    public MockClock()
    {
    }

    public MockClock(DateTime now)
    {
        _utcNow = now.ToUniversalTime();
    }


    public DateTime UtcNow
    {
        get => _utcNow;
        set
        {
            if (value < _utcNow)
                throw new InvalidOperationException("Time travel is one-way only.");
            value = value.ToUniversalTime();
            var diff = value - _utcNow;
            _utcNow = value;
            TimeChanged?.Invoke(diff);
        }
    }

    public event Action<TimeSpan>? TimeChanged;
}