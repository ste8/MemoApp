namespace MemoApp.UI.MauiApp.Messages;

/// <summary>
/// Message sent when a custom association is successfully deleted
/// </summary>
/// <param name="PackId">The pack ID where the association was deleted</param>
/// <param name="Category">The category of the deleted association</param>
/// <param name="Token">The token for which the association was deleted</param>
/// <param name="Word">The word of the deleted association</param>
/// <param name="WasDefault">Whether the deleted association was the default</param>
public record CustomAssociationDeletedMessage(
    string PackId,
    string Category,
    string Token,
    string Word,
    bool WasDefault);