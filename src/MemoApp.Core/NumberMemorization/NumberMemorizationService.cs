using System.Text;

namespace MemoApp.Core.NumberMemorization;

public class NumberMemorizationService : INumberMemorizationService
{
    private readonly Random _random = new();

    public NumberMemorizationGame CreateNewGame(NumberMemorizationSettings settings)
    {
        var game = new NumberMemorizationGame
        {
            GeneratedNumber = GenerateNumber(settings.NumberOfDigits, settings.MaxPairValue),
            Phase = GamePhase.Setup,
            IsTimerRunning = false,
            IsNumberVisible = true,
            ElapsedTime = TimeSpan.Zero,
            StartTime = null
        };
        
        return game;
    }

    public string GenerateNumber(int digits, int maxPairValue)
    {
        if (digits <= 0 || maxPairValue < 10 || maxPairValue > 99)
        {
            throw new ArgumentException("Invalid parameters for number generation");
        }

        var result = new StringBuilder();
        var isOddDigits = digits % 2 == 1;
        var pairCount = digits / 2;

        // Generate pairs
        for (int i = 0; i < pairCount; i++)
        {
            // First pair should not start with 0
            var minValue = (i == 0 && !isOddDigits) ? 10 : 0;
            var pair = _random.Next(minValue, maxPairValue + 1);
            result.Append(pair.ToString("D2"));
        }

        // Handle odd number of digits - add single digit at the beginning
        if (isOddDigits)
        {
            var firstDigit = _random.Next(1, 10); // 1-9, not 0
            result.Insert(0, firstDigit.ToString());
        }

        return result.ToString();
    }

    public void StartGame(NumberMemorizationGame game)
    {
        game.Phase = GamePhase.Memorizing;
        game.IsTimerRunning = true;
        game.IsNumberVisible = true;
        game.StartTime = DateTime.Now;
        game.ElapsedTime = TimeSpan.Zero;
    }

    public void StopGame(NumberMemorizationGame game)
    {
        game.Phase = GamePhase.Stopped;
        game.IsTimerRunning = false;
        game.IsNumberVisible = false;
        
        if (game.StartTime.HasValue)
        {
            game.ElapsedTime = DateTime.Now - game.StartTime.Value;
        }
    }

    public void ResetGame(NumberMemorizationGame game)
    {
        game.Phase = GamePhase.Setup;
        game.IsTimerRunning = false;
        game.IsNumberVisible = true;
        game.ElapsedTime = TimeSpan.Zero;
        game.StartTime = null;
        game.GeneratedNumber = string.Empty;
    }

    public void ToggleNumberVisibility(NumberMemorizationGame game)
    {
        game.IsNumberVisible = !game.IsNumberVisible;
    }

    public string FormatNumber(string number, bool showSeparated)
    {
        if (string.IsNullOrEmpty(number))
            return string.Empty;

        if (!showSeparated)
            return number;

        var result = new StringBuilder();
        
        // If odd length, first digit is single
        var startIndex = 0;
        if (number.Length % 2 == 1)
        {
            result.Append(number[0]);
            startIndex = 1;
        }

        // Add pairs with spaces
        for (int i = startIndex; i < number.Length; i += 2)
        {
            if (result.Length > 0)
                result.Append(' ');
            
            result.Append(number[i]);
            if (i + 1 < number.Length)
                result.Append(number[i + 1]);
        }

        return result.ToString();
    }

    public string FormatTime(TimeSpan timeSpan)
    {
        return $"{(int)timeSpan.TotalMinutes:D2}:{timeSpan.Seconds:D2}";
    }
}