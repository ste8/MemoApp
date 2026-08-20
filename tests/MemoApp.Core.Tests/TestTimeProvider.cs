namespace MemoApp.Core.Tests;

/// <summary>
/// A clock the tests drive by hand.
///
/// <see cref="Advance"/> moves wall-clock and monotonic time together, as real time passing
/// does. <see cref="ShiftWallClock"/> moves only the wall clock, reproducing what a timezone
/// update or an NTP correction does to a device mid-session: readings from
/// <see cref="TimeProvider.GetUtcNow"/> jump, while the monotonic timestamp does not.
/// </summary>
internal sealed class TestTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;
    private long _timestamp;

    public TestTimeProvider(DateTimeOffset start)
    {
        _utcNow = start;
    }

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public override long GetTimestamp() => _timestamp;

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public void Advance(TimeSpan by)
    {
        _utcNow = _utcNow.Add(by);
        _timestamp += by.Ticks;
    }

    public void ShiftWallClock(TimeSpan by) => _utcNow = _utcNow.Add(by);
}
