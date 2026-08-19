using MemoApp.Core.BigNumber;

namespace MemoApp.UI.MauiApp.Services;

public class BigNumberSettingsService : IBigNumberSettingsService
{
    private const string NumberOfDigitsKey = "BigNumber_NumberOfDigits";
    private const string MaxPairValueKey = "BigNumber_MaxPairValue";
    private const string ShowSeparatedKey = "BigNumber_ShowSeparated";
    private const string ShowTimerKey = "BigNumber_ShowTimer";
    private const string EnableInputVerificationKey = "BigNumber_EnableInputVerification";
    private const string VerificationModeKey = "BigNumber_VerificationMode";
    private const string FontSizeKey = "BigNumber_FontSize";

    public Task<BigNumberSettings> LoadSettingsAsync()
    {
        var settings = new BigNumberSettings
        {
            NumberOfDigits = Preferences.Get(NumberOfDigitsKey, 20),
            MaxPairValue = Preferences.Get(MaxPairValueKey, 99),
            ShowSeparated = Preferences.Get(ShowSeparatedKey, false),
            ShowTimer = Preferences.Get(ShowTimerKey, false),
            EnableInputVerification = Preferences.Get(EnableInputVerificationKey, false),
            VerificationMode = (VerificationModePreference)Preferences.Get(VerificationModeKey, (int)VerificationModePreference.InApp),
            FontSize = (FontSizePreference)Preferences.Get(FontSizeKey, (int)FontSizePreference.Auto)
        };

        return Task.FromResult(settings);
    }

    public Task SaveSettingsAsync(BigNumberSettings settings)
    {
        Preferences.Set(NumberOfDigitsKey, settings.NumberOfDigits);
        Preferences.Set(MaxPairValueKey, settings.MaxPairValue);
        Preferences.Set(ShowSeparatedKey, settings.ShowSeparated);
        Preferences.Set(ShowTimerKey, settings.ShowTimer);
        Preferences.Set(EnableInputVerificationKey, settings.EnableInputVerification);
        Preferences.Set(VerificationModeKey, (int)settings.VerificationMode);
        Preferences.Set(FontSizeKey, (int)settings.FontSize);

        return Task.CompletedTask;
    }
}