namespace MemoApp.UI.MauiApp.Models.Freemium;

public class AchievementMilestone
{
    public int ExerciseCountRequired { get; set; }
    public int DiscountPercentage { get; set; }
    public DateTime? FirstShownDate { get; set; }
    public bool HasExpired => FirstShownDate.HasValue && 
                              (DateTime.UtcNow - FirstShownDate.Value).TotalDays > 7;
    public bool WasCompleted { get; set; }
    
    public static List<AchievementMilestone> GetDefaultMilestones()
    {
        return new List<AchievementMilestone>
        {
            new() { ExerciseCountRequired = 20, DiscountPercentage = 25 },
            new() { ExerciseCountRequired = 50, DiscountPercentage = 30 },
            new() { ExerciseCountRequired = 100, DiscountPercentage = 35 }
        };
    }
}