using MemoApp.Core.BigNumber;

namespace MemoApp.UI.MauiApp.Services;

public class BigNumberSettingsService : IBigNumberSettingsService
{
    private const string NumberOfDigitsKey = "BigNumber_NumberOfDigits";
    private const string MaxPairValueKey = "BigNumber_MaxPairValue";
    private const string ShowSeparatedKey = "BigNumber_ShowSeparated";
    private const string ShowTimerKey = "BigNumber_ShowTimer";
    private const string ShowAssociationsKey = "BigNumber_ShowAssociations";
    private const string EnableInputVerificationKey = "BigNumber_EnableInputVerification";

    public Task<BigNumberSettings> LoadSettingsAsync()
    {
        var settings = new BigNumberSettings
        {
            NumberOfDigits = Preferences.Get(NumberOfDigitsKey, 20),
            MaxPairValue = Preferences.Get(MaxPairValueKey, 99),
            ShowSeparated = Preferences.Get(ShowSeparatedKey, false),
            ShowTimer = Preferences.Get(ShowTimerKey, false),
            ShowAssociations = Preferences.Get(ShowAssociationsKey, false),
            EnableInputVerification = Preferences.Get(EnableInputVerificationKey, false)
        };

        return Task.FromResult(settings);
    }

    public Task SaveSettingsAsync(BigNumberSettings settings)
    {
        Preferences.Set(NumberOfDigitsKey, settings.NumberOfDigits);
        Preferences.Set(MaxPairValueKey, settings.MaxPairValue);
        Preferences.Set(ShowSeparatedKey, settings.ShowSeparated);
        Preferences.Set(ShowTimerKey, settings.ShowTimer);
        Preferences.Set(ShowAssociationsKey, settings.ShowAssociations);
        Preferences.Set(EnableInputVerificationKey, settings.EnableInputVerification);

        return Task.CompletedTask;
    }
}