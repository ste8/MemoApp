namespace MemoApp.Core.MajorSystem.Models;

/// <summary>
/// Event arguments for when a token default is changed
/// </summary>
public class TokenDefaultChangedEventArgs : EventArgs
{
    /// <summary>
    /// The pack ID where the default was changed
    /// </summary>
    public string PackId { get; set; } = string.Empty;
    
    /// <summary>
    /// The category where the default was changed
    /// </summary>
    public string Category { get; set; } = string.Empty;
    
    /// <summary>
    /// The token that had its default changed
    /// </summary>
    public string Token { get; set; } = string.Empty;
    
    /// <summary>
    /// The new default filename
    /// </summary>
    public string NewDefault { get; set; } = string.Empty;
}