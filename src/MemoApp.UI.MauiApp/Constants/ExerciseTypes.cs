namespace MemoApp.UI.MauiApp.Constants;

/// <summary>
/// Contains constants for all exercise type identifiers used for ads and counting.
/// This provides a centralized way to reference exercise types without using magic strings.
/// </summary>
public static class ExerciseTypes
{
    /// <summary>
    /// Flash Numbers exercise type identifier
    /// </summary>
    public const string FlashNumbers = "FlashNumbers";
    
    /// <summary>
    /// Big Number exercise type identifier
    /// </summary>
    public const string BigNumber = "BigNumber";
    
    /// <summary>
    /// Gets all available exercise identifiers
    /// </summary>
    public static readonly string[] All = { FlashNumbers, BigNumber };
}