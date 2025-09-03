namespace MemoApp.Core.BigNumber;

public interface IBigNumberSettingsService
{
    Task<BigNumberSettings> LoadSettingsAsync();
    Task SaveSettingsAsync(BigNumberSettings settings);
}