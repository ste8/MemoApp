using MemoApp.Core.MajorSystem;

namespace MemoApp.Core.Tests;

public class GameSessionTests
{
    [Fact]
    public void Constructor_ValidRange_InitializesCorrectly()
    {
        var session = new GameSession("00", "05");
        
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
        var session = new GameSession("0", "2");
        
        session.StartSession();
        
        Assert.Equal(SessionState.InProgress, session.State);
        Assert.NotNull(session.CurrentNumber);
        Assert.Equal(0, session.CompletedNumbers);
        Assert.Equal(3, session.RemainingNumbers);
    }

    [Fact]
    public void StartSession_AlreadyStarted_ThrowsException()
    {
        var session = new GameSession("0", "1");
        session.StartSession();
        
        Assert.Throws<InvalidOperationException>(() => session.StartSession());
    }

    [Fact]
    public void AdvanceToNext_InProgress_AdvancesCorrectly()
    {
        var session = new GameSession("0", "1");
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
        var session = new GameSession("0", "0");
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
        var session = new GameSession("0", "1");
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
        var session = new GameSession("0", "1");
        session.StartSession();
        
        Assert.Throws<InvalidOperationException>(() => session.GetStatistics());
    }
}