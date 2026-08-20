using System.Text.Json.Serialization;
using MemoApp.Core.BigNumber;
using MemoApp.Core.FlashNumbers;

namespace MemoApp.Core.Services.Settings;

/// <summary>
/// Source-generated serialization for the settings models, so persisting them does not rely
/// on reflection and is safe under trimming and AOT.
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(BigNumberSettings))]
[JsonSerializable(typeof(FlashNumbersSettings))]
public partial class SettingsJsonContext : JsonSerializerContext
{
}
