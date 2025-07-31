using MemoApp.Core.NumberMemorization;

namespace MemoApp.UI.MauiApp.Services;

public class NumberMemorizationSettingsService : INumberMemorizationSettingsService
{
    private const string NumberOfDigitsKey = "NumberMemorization_NumberOfDigits";
    private const string MaxPairValueKey = "NumberMemorization_MaxPairValue";
    private const string ShowSeparatedKey = "NumberMemorization_ShowSeparated";
    private const string ShowTimerKey = "NumberMemorization_ShowTimer";

    public Task<NumberMemorizationSettings> LoadSettingsAsync()
    {
        var settings = new NumberMemorizationSettings
        {
            NumberOfDigits = Preferences.Get(NumberOfDigitsKey, 20),
            MaxPairValue = Preferences.Get(MaxPairValueKey, 99),
            ShowSeparated = Preferences.Get(ShowSeparatedKey, false),
            ShowTimer = Preferences.Get(ShowTimerKey, false)
        };

        return Task.FromResult(settings);
    }

    public Task SaveSettingsAsync(NumberMemorizationSettings settings)
    {
        Preferences.Set(NumberOfDigitsKey, settings.NumberOfDigits);
        Preferences.Set(MaxPairValueKey, settings.MaxPairValue);
        Preferences.Set(ShowSeparatedKey, settings.ShowSeparated);
        Preferences.Set(ShowTimerKey, settings.ShowTimer);

        return Task.CompletedTask;
    }
}