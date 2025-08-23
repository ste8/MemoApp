namespace MemoApp.UI.MauiApp.Messages;

/// <summary>
/// Message sent when a token's default association is changed
/// </summary>
/// <param name="PackId">The pack ID where the default was changed</param>
/// <param name="Category">The category of the token</param>
/// <param name="Token">The token whose default was changed</param>
/// <param name="NewDefault">The filename of the new default association</param>
/// <param name="PreviousDefault">The filename of the previous default association (if any)</param>
public record TokenDefaultChangedMessage(
    string PackId,
    string Category,
    string Token,
    string NewDefault,
    string? PreviousDefault);