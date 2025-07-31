namespace MemoApp.Core.NumberMemorization;

public interface INumberMemorizationSettingsService
{
    Task<NumberMemorizationSettings> LoadSettingsAsync();
    Task SaveSettingsAsync(NumberMemorizationSettings settings);
}