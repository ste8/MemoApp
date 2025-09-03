namespace MemoApp.Core.BigNumber;

public interface IBigNumberService
{
    BigNumberGame CreateNewGame(BigNumberSettings settings);
    string GenerateNumber(int digits, int maxPairValue);
    void StartGame(BigNumberGame game);
    void StopGame(BigNumberGame game);
    void ResetGame(BigNumberGame game);
    void ToggleNumberVisibility(BigNumberGame game);
    string FormatNumber(string number, bool showSeparated);
    string FormatTime(TimeSpan timeSpan);
}