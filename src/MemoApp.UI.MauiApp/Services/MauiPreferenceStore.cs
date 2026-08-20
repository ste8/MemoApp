using MemoApp.Core.Services.Settings;

namespace MemoApp.UI.MauiApp.Services;

/// <summary>
/// Platform implementation of <see cref="IPreferenceStore"/> over MAUI's static Preferences.
/// Deliberately thin: it exists only so the settings services can live in Core.
/// </summary>
public class MauiPreferenceStore : IPreferenceStore
{
    public int Get(string key, int defaultValue) => Preferences.Get(key, defaultValue);

    public bool Get(string key, bool defaultValue) => Preferences.Get(key, defaultValue);

    public string Get(string key, string defaultValue) => Preferences.Get(key, defaultValue);

    public void Set(string key, int value) => Preferences.Set(key, value);

    public void Set(string key, bool value) => Preferences.Set(key, value);

    public void Set(string key, string value) => Preferences.Set(key, value);
}
