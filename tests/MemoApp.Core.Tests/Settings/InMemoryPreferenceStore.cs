using MemoApp.Core.Services.Settings;

namespace MemoApp.Core.Tests.Settings;

/// <summary>In-memory <see cref="IPreferenceStore"/> for tests.</summary>
internal sealed class InMemoryPreferenceStore : IPreferenceStore
{
    private readonly Dictionary<string, object> _values = new();

    public int Get(string key, int defaultValue) => _values.TryGetValue(key, out var v) ? (int)v : defaultValue;

    public bool Get(string key, bool defaultValue) => _values.TryGetValue(key, out var v) ? (bool)v : defaultValue;

    public string Get(string key, string defaultValue) => _values.TryGetValue(key, out var v) ? (string)v : defaultValue;

    public void Set(string key, int value) => _values[key] = value;

    public void Set(string key, bool value) => _values[key] = value;

    public void Set(string key, string value) => _values[key] = value;
}
