namespace MemoApp.Localization.Services;

/// <summary>
/// Represents the different number format options for the Major System.
/// </summary>
public enum NumberFormat
{
    /// <summary>
    /// Padded format with leading zeros (00, 01, 02, ..., 99).
    /// Default for new users.
    /// </summary>
    Padded = 0,
    
    /// <summary>
    /// Natural format with no leading zeros (0, 1, 2, ..., 99).
    /// </summary>
    Natural = 1
}