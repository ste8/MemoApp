namespace MemoApp.Core.NumberMemorization;

public interface INumberMemorizationService
{
    NumberMemorizationGame CreateNewGame(NumberMemorizationSettings settings);
    string GenerateNumber(int digits, int maxPairValue);
    void StartGame(NumberMemorizationGame game);
    void StopGame(NumberMemorizationGame game);
    void ResetGame(NumberMemorizationGame game);
    void ToggleNumberVisibility(NumberMemorizationGame game);
    string FormatNumber(string number, bool showSeparated);
    string FormatTime(TimeSpan timeSpan);
}