namespace MemoApp.UI.MauiApp.Messages;

/// <summary>
/// Message sent when a custom association is successfully added
/// </summary>
/// <param name="PackId">The pack ID where the association was added</param>
/// <param name="Category">The category of the association</param>
/// <param name="Token">The token for which the association was added</param>
/// <param name="Word">The word of the new association</param>
/// <param name="WasSetAsDefault">Whether the new association was set as default</param>
public record CustomAssociationAddedMessage(
    string PackId,
    string Category,
    string Token,
    string Word,
    bool WasSetAsDefault);