using MemoApp.Core.Services.Settings;

namespace MemoApp.Core.FlashNumbers;

public class FlashNumbersSettingsService : IFlashNumbersSettingsService
{
    private const string SettingsKey = "FlashNumbers_Settings";

    private readonly JsonSettingsStore<FlashNumbersSettings> _store;

    public FlashNumbersSettingsService(IPreferenceStore preferences)
    {
        _store = new JsonSettingsStore<FlashNumbersSettings>(
            preferences, SettingsKey, SettingsJsonContext.Default.FlashNumbersSettings);
    }

    public Task<FlashNumbersSettings> LoadSettingsAsync() => _store.LoadAsync();

    public Task SaveSettingsAsync(FlashNumbersSettings settings) => _store.SaveAsync(settings);
}
