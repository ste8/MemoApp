namespace MemoApp.Core.NumberMemorization;

public class NumberMemorizationGame
{
    public string GeneratedNumber { get; set; } = string.Empty;
    public TimeSpan ElapsedTime { get; set; } = TimeSpan.Zero;
    public GamePhase Phase { get; set; } = GamePhase.Setup;
    public bool IsTimerRunning { get; set; }
    public bool IsNumberVisible { get; set; }
    public DateTime? StartTime { get; set; }
}