namespace MemoApp.Core.NumberMemorization;

public class NumberMemorizationSettings
{
    public int NumberOfDigits { get; set; } = 20;
    public int MaxPairValue { get; set; } = 99;
    public bool ShowSeparated { get; set; } = false;
    public bool ShowTimer { get; set; } = false;
}