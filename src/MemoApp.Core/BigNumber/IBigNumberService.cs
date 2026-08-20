namespace MemoApp.Core.BigNumber;

public interface IBigNumberService
{
    BigNumberGame CreateNewGame(BigNumberSettings settings);
    string GenerateNumber(int digits, int maxPairValue);
    void StartGame(BigNumberGame game);
    void StopGame(BigNumberGame game);
    void ResetGame(BigNumberGame game);
    void ToggleNumberVisibility(BigNumberGame game);
    void StartRecall(BigNumberGame game);
    string FormatNumber(string number, bool showSeparated);
    string FormatTime(TimeSpan timeSpan);

    /// <summary>
    /// Elapsed time for a running round, measured from the monotonic start timestamp.
    /// Returns the recorded ElapsedTime once the round has stopped.
    /// </summary>
    TimeSpan GetElapsedTime(BigNumberGame game);
}