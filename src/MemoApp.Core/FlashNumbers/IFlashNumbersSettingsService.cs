namespace MemoApp.Core.FlashNumbers;

public interface IFlashNumbersSettingsService
{
    Task<FlashNumbersSettings> LoadSettingsAsync();
    Task SaveSettingsAsync(FlashNumbersSettings settings);
}