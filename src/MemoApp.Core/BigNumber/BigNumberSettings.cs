namespace MemoApp.Core.BigNumber;

public class BigNumberSettings
{
    public int NumberOfDigits { get; set; } = 20;
    public int MaxPairValue { get; set; } = 99;
    public bool ShowSeparated { get; set; } = false;
    public bool ShowTimer { get; set; } = false;
    public FontSizePreference FontSize { get; set; } = FontSizePreference.Auto;
    public bool ShowAssociations { get; set; } = false;
    public bool EnableInputVerification { get; set; } = false;
}