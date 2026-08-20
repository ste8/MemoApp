namespace MemoApp.Core.BigNumber;

public class BigNumberGame
{
    public string GeneratedNumber { get; set; } = string.Empty;
    public TimeSpan ElapsedTime { get; set; } = TimeSpan.Zero;
    public GamePhase Phase { get; set; } = GamePhase.Setup;
    public bool IsTimerRunning { get; set; }
    public bool IsNumberVisible { get; set; }
    /// <summary>Wall-clock start, in UTC. For timestamps and analytics only.</summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// Monotonic timestamp captured when the round started, used to measure elapsed time.
    /// Unlike wall-clock arithmetic this is unaffected by the device clock moving - a
    /// timezone update mid-round cannot make the timer jump or run backwards.
    /// </summary>
    public long StartTimestamp { get; set; }
}