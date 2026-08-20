using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace MemoApp.Core.Services.Settings;

/// <summary>
/// Persists a settings object as a single JSON payload under one preference key.
///
/// Settings are stored whole rather than property-by-property, so a property added to the
/// model is persisted automatically. The previous per-property approach required every new
/// property to be wired into both a load and a save method by hand, and a property that was
/// missed appeared to work until the next launch.
///
/// Serialization is source-generated via a <see cref="JsonTypeInfo{T}"/> supplied by the
/// caller, so it survives trimming and AOT on iOS.
/// </summary>
public class JsonSettingsStore<T> where T : class, new()
{
    private readonly IPreferenceStore _preferences;
    private readonly string _key;
    private readonly JsonTypeInfo<T> _typeInfo;

    public JsonSettingsStore(IPreferenceStore preferences, string key, JsonTypeInfo<T> typeInfo)
    {
        _preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        _key = key ?? throw new ArgumentNullException(nameof(key));
        _typeInfo = typeInfo ?? throw new ArgumentNullException(nameof(typeInfo));
    }

    /// <summary>
    /// Returns the stored settings, or a default instance when nothing is stored or the
    /// stored payload cannot be read. Corrupt settings must not prevent the app from
    /// opening the screen that owns them.
    /// </summary>
    public Task<T> LoadAsync()
    {
        var payload = _preferences.Get(_key, string.Empty);

        if (string.IsNullOrWhiteSpace(payload))
        {
            return Task.FromResult(new T());
        }

        try
        {
            return Task.FromResult(JsonSerializer.Deserialize(payload, _typeInfo) ?? new T());
        }
        catch (JsonException)
        {
            return Task.FromResult(new T());
        }
    }

    public Task SaveAsync(T settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _preferences.Set(_key, JsonSerializer.Serialize(settings, _typeInfo));

        return Task.CompletedTask;
    }
}
