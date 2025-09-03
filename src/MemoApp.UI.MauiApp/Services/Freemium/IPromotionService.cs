using MemoApp.UI.MauiApp.Models.Freemium;

namespace MemoApp.UI.MauiApp.Services.Freemium;

public interface IPromotionService
{
    Task<PromotionData?> GetActivePromotionAsync();
    Task<AchievementMilestone?> GetCurrentAchievementMilestoneAsync();
    Task UpdateExerciseCountAsync();
    Task<int> GetTotalExerciseCountAsync();
    Task<List<SubscriptionOption>> ApplyPromotionsToSubscriptionsAsync(List<SubscriptionOption> subscriptions);
    void PauseLocalPromotionTimers();
    void ResumeLocalPromotionTimers();
}