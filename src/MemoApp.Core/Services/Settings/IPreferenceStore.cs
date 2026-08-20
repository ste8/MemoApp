namespace MemoApp.Core.Services.Settings;

/// <summary>
/// Key/value persistence for user settings.
///
/// Exists so settings services can live in Core and be unit-tested. The platform
/// implementation wraps MAUI's static <c>Preferences</c>, which cannot be substituted
/// in a test.
/// </summary>
public interface IPreferenceStore
{
    int Get(string key, int defaultValue);
    bool Get(string key, bool defaultValue);
    string Get(string key, string defaultValue);

    void Set(string key, int value);
    void Set(string key, bool value);
    void Set(string key, string value);
}
