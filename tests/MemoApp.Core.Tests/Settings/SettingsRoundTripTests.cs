using System.Reflection;
using MemoApp.Core.BigNumber;
using MemoApp.Core.FlashNumbers;

namespace MemoApp.Core.Tests.Settings;

/// <summary>
/// Settings must survive a save/load cycle in full.
///
/// The reflection-driven tests below assign a non-default value to every public property on
/// the settings model, save, reload, and require each one back. A property that the service
/// forgets to persist fails here rather than reaching a user as a setting that silently
/// resets on every launch.
/// </summary>
public class SettingsRoundTripTests
{
    [Fact]
    public async Task BigNumberSettings_EveryPropertyIsPersisted()
    {
        var service = new BigNumberSettingsService(new InMemoryPreferenceStore());
        var settings = new BigNumberSettings();
        var expected = MutateEveryProperty(settings);

        await service.SaveSettingsAsync(settings);
        var reloaded = await service.LoadSettingsAsync();

        AssertAllPropertiesMatch(expected, reloaded);
    }

    [Fact]
    public async Task FlashNumbersSettings_EveryPropertyIsPersisted()
    {
        var service = new FlashNumbersSettingsService(new InMemoryPreferenceStore());
        var settings = new FlashNumbersSettings();
        var expected = MutateEveryProperty(settings);

        await service.SaveSettingsAsync(settings);
        var reloaded = await service.LoadSettingsAsync();

        AssertAllPropertiesMatch(expected, reloaded);
    }

    [Fact]
    public async Task BigNumberSettings_WithNothingStored_ReturnsModelDefaults()
    {
        var service = new BigNumberSettingsService(new InMemoryPreferenceStore());

        var loaded = await service.LoadSettingsAsync();

        AssertAllPropertiesMatch(Snapshot(new BigNumberSettings()), loaded);
    }

    [Fact]
    public async Task FlashNumbersSettings_WithNothingStored_ReturnsModelDefaults()
    {
        var service = new FlashNumbersSettingsService(new InMemoryPreferenceStore());

        var loaded = await service.LoadSettingsAsync();

        AssertAllPropertiesMatch(Snapshot(new FlashNumbersSettings()), loaded);
    }

    [Theory]
    [InlineData("not json at all")]
    [InlineData("{ \"NumberOfDigits\": ")]
    [InlineData("[]")]
    public async Task BigNumberSettings_WithAnUnreadablePayload_FallsBackToDefaults(string payload)
    {
        // A corrupt payload must not throw: the screen that owns these settings would then
        // fail to open every time, with no way back short of reinstalling.
        var preferences = new InMemoryPreferenceStore();
        preferences.Set("BigNumber_Settings", payload);
        var service = new BigNumberSettingsService(preferences);

        var loaded = await service.LoadSettingsAsync();

        AssertAllPropertiesMatch(Snapshot(new BigNumberSettings()), loaded);
    }

    [Fact]
    public async Task Settings_SurviveASecondRoundTrip()
    {
        var preferences = new InMemoryPreferenceStore();
        var service = new BigNumberSettingsService(preferences);
        var settings = new BigNumberSettings();
        var expected = MutateEveryProperty(settings);

        await service.SaveSettingsAsync(settings);
        var once = await service.LoadSettingsAsync();
        await service.SaveSettingsAsync(once);
        var twice = await service.LoadSettingsAsync();

        AssertAllPropertiesMatch(expected, twice);
    }

    private static IReadOnlyDictionary<string, object?> Snapshot(object instance)
        => Properties(instance.GetType()).ToDictionary(p => p.Name, p => p.GetValue(instance));

    /// <summary>
    /// Sets every property to something other than its default so an unpersisted property
    /// cannot pass by coincidence.
    /// </summary>
    private static IReadOnlyDictionary<string, object?> MutateEveryProperty(object instance)
    {
        foreach (var property in Properties(instance.GetType()))
        {
            property.SetValue(instance, DistinctValueFor(property, property.GetValue(instance)));
        }

        return Snapshot(instance);
    }

    private static object DistinctValueFor(PropertyInfo property, object? current)
    {
        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        if (type.IsEnum)
        {
            var values = Enum.GetValues(type).Cast<object>().ToList();
            return values.FirstOrDefault(v => !Equals(v, current)) ?? values[0];
        }

        if (type == typeof(int)) return (int)(current ?? 0) + 7;
        if (type == typeof(bool)) return !(bool)(current ?? false);
        if (type == typeof(string)) return $"{current}-changed";

        throw new NotSupportedException(
            $"{property.DeclaringType?.Name}.{property.Name} is {type.Name}; extend this helper so the property is covered.");
    }

    private static void AssertAllPropertiesMatch(IReadOnlyDictionary<string, object?> expected, object actual)
    {
        foreach (var property in Properties(actual.GetType()))
        {
            Assert.Equal(expected[property.Name], property.GetValue(actual));
        }
    }

    private static IEnumerable<PropertyInfo> Properties(Type type)
        => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
               .Where(p => p is { CanRead: true, CanWrite: true });
}
