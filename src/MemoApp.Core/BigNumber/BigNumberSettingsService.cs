using MemoApp.Core.Services.Settings;

namespace MemoApp.Core.BigNumber;

public class BigNumberSettingsService : IBigNumberSettingsService
{
    private const string SettingsKey = "BigNumber_Settings";

    private readonly JsonSettingsStore<BigNumberSettings> _store;

    public BigNumberSettingsService(IPreferenceStore preferences)
    {
        _store = new JsonSettingsStore<BigNumberSettings>(
            preferences, SettingsKey, SettingsJsonContext.Default.BigNumberSettings);
    }

    public Task<BigNumberSettings> LoadSettingsAsync() => _store.LoadAsync();

    public Task SaveSettingsAsync(BigNumberSettings settings) => _store.SaveAsync(settings);
}
