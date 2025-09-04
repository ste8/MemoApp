namespace MemoApp.UI.MauiApp.Services.Freemium;

public interface IInterstitialAdManager
{
    Task PrepareAdForExerciseAsync(string exerciseType);
    Task<bool> ShouldShowAdForExerciseAsync(string exerciseType);
    Task ShowAdIfNeededAsync(string exerciseType);
}