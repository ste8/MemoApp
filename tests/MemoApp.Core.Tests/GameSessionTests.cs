using MemoApp.Core.MajorSystem;

namespace MemoApp.Core.Tests;

public class GameSessionTests
{
    [Fact]
    public void Constructor_ValidRange_InitializesCorrectly()
    {
        var session = new GameSession("00", "05", NumberFormat.Padded);
        
        Assert.Equal("00", session.RangeStart);
        Assert.Equal("05", session.RangeEnd);
        Assert.Equal(SessionState.NotStarted, session.State);
        Assert.Equal(6, session.TotalNumbers);
        Assert.Equal(0, session.CompletedNumbers);
        Assert.Equal(6, session.RemainingNumbers);
        Assert.Null(session.CurrentNumber);
    }

    [Fact]
    public void StartSession_NotStartedState_StartsSuccessfully()
    {
        var session = new GameSession("0", "2", NumberFormat.Padded);
        
        session.StartSession();
        
        Assert.Equal(SessionState.InProgress, session.State);
        Assert.NotNull(session.CurrentNumber);
        Assert.Equal(0, session.CompletedNumbers);
        Assert.Equal(3, session.RemainingNumbers);
    }

    [Fact]
    public void StartSession_AlreadyStarted_ThrowsException()
    {
        var session = new GameSession("0", "1", NumberFormat.Padded);
        session.StartSession();
        
        Assert.Throws<InvalidOperationException>(() => session.StartSession());
    }

    [Fact]
    public void AdvanceToNext_InProgress_AdvancesCorrectly()
    {
        var session = new GameSession("0", "1", NumberFormat.Padded);
        session.StartSession();
        var firstNumber = session.CurrentNumber;
        
        Thread.Sleep(10); // Ensure some time passes
        session.AdvanceToNext();
        
        Assert.Equal(1, session.CompletedNumbers);
        Assert.Equal(1, session.RemainingNumbers);
        Assert.NotEqual(firstNumber, session.CurrentNumber);
        Assert.Single(session.Performances);
    }

    [Fact]
    public void AdvanceToNext_LastNumber_CompletesSession()
    {
        var session = new GameSession("0", "0", NumberFormat.Padded);
        session.StartSession();
        
        Thread.Sleep(10);
        session.AdvanceToNext();
        
        Assert.Equal(SessionState.Completed, session.State);
        Assert.Equal(1, session.CompletedNumbers);
        Assert.Equal(0, session.RemainingNumbers);
        Assert.Null(session.CurrentNumber);
    }

    [Fact]
    public void GetStatistics_CompletedSession_ReturnsValidStatistics()
    {
        var session = new GameSession("0", "1", NumberFormat.Padded);
        session.StartSession();
        
        Thread.Sleep(10);
        session.AdvanceToNext();
        Thread.Sleep(10);
        session.AdvanceToNext();
        
        var stats = session.GetStatistics();
        
        Assert.Equal(2, stats.TotalNumbers);
        Assert.True(stats.TotalDuration > TimeSpan.Zero);
        Assert.True(stats.AverageResponseTime > TimeSpan.Zero);
        Assert.Equal(2, stats.SlowestResponses.Count);
    }

    [Fact]
    public void GetStatistics_NotCompleted_ThrowsException()
    {
        var session = new GameSession("0", "1", NumberFormat.Padded);
        session.StartSession();
        
        Assert.Throws<InvalidOperationException>(() => session.GetStatistics());
    }

    [Fact]
    public void Constructor_FullRange_PresentsOneNumberPerLabel()
    {
        var session = new GameSession("00", "99", NumberFormat.Padded);

        Assert.Equal(100, session.TotalNumbers);
    }

    [Fact]
    public void Constructor_SuppliedSequence_PresentsExactlyThatSequence()
    {
        var sequence = NumberSequence.GenerateSequence("00", "04", NumberFormat.Padded).ToList();

        var session = new GameSession("00", "04", sequence);

        Assert.Equal(5, session.TotalNumbers);
    }

    [Fact]
    public void Constructor_ShufflesWithoutLosingOrRepeatingANumber()
    {
        var sequence = NumberSequence.GenerateSequence("00", "99", NumberFormat.Padded).ToList();

        var session = new GameSession("00", "99", sequence);
        session.StartSession();

        var presented = new List<MajorNumber>();
        while (session.CurrentNumber.HasValue)
        {
            presented.Add(session.CurrentNumber.Value);
            session.AdvanceToNext();
        }

        Assert.Equal(sequence.OrderBy(n => n.Value), presented.OrderBy(n => n.Value));
    }

    // ---------------------------------------------------------------
    // Elapsed time
    // ---------------------------------------------------------------

    [Fact]
    public void Elapsed_BeforeStarting_IsZero()
    {
        var session = new GameSession("00", "02", NumberFormat.Padded);

        Assert.Equal(TimeSpan.Zero, session.Elapsed);
    }

    [Fact]
    public void Elapsed_WhileInProgress_TracksRealTimePassing()
    {
        var clock = new TestTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var session = new GameSession("00", "02", NumberFormat.Padded, clock);

        session.StartSession();
        clock.Advance(TimeSpan.FromSeconds(90));

        Assert.Equal(TimeSpan.FromSeconds(90), session.Elapsed);
    }

    [Fact]
    public void Elapsed_WhenTheDeviceClockJumps_IsUnaffected()
    {
        var clock = new TestTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var session = new GameSession("00", "02", NumberFormat.Padded, clock);

        session.StartSession();
        clock.Advance(TimeSpan.FromSeconds(40));
        clock.ShiftWallClock(TimeSpan.FromHours(-1));

        Assert.Equal(TimeSpan.FromSeconds(40), session.Elapsed);
    }

    [Fact]
    public void Elapsed_AfterCompletion_StopsAdvancing()
    {
        var clock = new TestTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var session = new GameSession("00", "01", NumberFormat.Padded, clock);

        session.StartSession();
        clock.Advance(TimeSpan.FromSeconds(5));
        session.AdvanceToNext();
        clock.Advance(TimeSpan.FromSeconds(5));
        session.AdvanceToNext();

        var atCompletion = session.Elapsed;
        clock.Advance(TimeSpan.FromMinutes(10));

        Assert.Equal(SessionState.Completed, session.State);
        Assert.Equal(TimeSpan.FromSeconds(10), atCompletion);
        Assert.Equal(atCompletion, session.Elapsed);
    }

    [Fact]
    public void GetStatistics_RecordsResponseTimeFromTheInjectedClock()
    {
        var clock = new TestTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var session = new GameSession("00", "01", NumberFormat.Padded, clock);

        session.StartSession();
        clock.Advance(TimeSpan.FromSeconds(3));
        session.AdvanceToNext();
        clock.Advance(TimeSpan.FromSeconds(7));
        session.AdvanceToNext();

        var statistics = session.GetStatistics();

        Assert.Equal(2, statistics.TotalNumbers);
        Assert.Equal(TimeSpan.FromSeconds(3), statistics.FastestResponse.ResponseTime);
    }
}
