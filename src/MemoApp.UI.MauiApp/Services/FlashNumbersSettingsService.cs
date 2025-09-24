using MemoApp.Core.FlashNumbers;

namespace MemoApp.UI.MauiApp.Services;

public class FlashNumbersSettingsService : IFlashNumbersSettingsService
{
    private const string CustomRangeStartIndexKey = "FlashNumbers_CustomRangeStartIndex";
    private const string CustomRangeEndIndexKey = "FlashNumbers_CustomRangeEndIndex";
    private const string CustomRangeStartKey = "FlashNumbers_CustomRangeStart";
    private const string CustomRangeEndKey = "FlashNumbers_CustomRangeEnd";

    public Task<FlashNumbersSettings> LoadSettingsAsync()
    {
        var settings = new FlashNumbersSettings
        {
            CustomRangeStartIndex = Preferences.Get(CustomRangeStartIndexKey, 0),
            CustomRangeEndIndex = Preferences.Get(CustomRangeEndIndexKey, 9),
            CustomRangeStart = Preferences.Get(CustomRangeStartKey, "00"),
            CustomRangeEnd = Preferences.Get(CustomRangeEndKey, "09")
        };

        return Task.FromResult(settings);
    }

    public Task SaveSettingsAsync(FlashNumbersSettings settings)
    {
        Preferences.Set(CustomRangeStartIndexKey, settings.CustomRangeStartIndex);
        Preferences.Set(CustomRangeEndIndexKey, settings.CustomRangeEndIndex);
        Preferences.Set(CustomRangeStartKey, settings.CustomRangeStart);
        Preferences.Set(CustomRangeEndKey, settings.CustomRangeEnd);

        return Task.CompletedTask;
    }
}