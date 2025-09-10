namespace MemoApp.Core.Services.Analytics;

/// <summary>
/// Standardized event names for Firebase Analytics following snake_case naming convention
/// </summary>
public static class AnalyticsEvents
{
    // App Lifecycle Events
    public const string AppStart = "app_start";
    public const string AppForeground = "app_foreground";
    public const string AppBackground = "app_background";
    public const string SettingsChanged = "settings_changed";
    public const string LanguageChanged = "language_changed";
    public const string NumberFormatChanged = "number_format_changed";
    
    // Exercise Events
    public const string ExerciseStarted = "exercise_started";
    public const string ExerciseCompleted = "exercise_completed";
    public const string ExerciseAbandoned = "exercise_abandoned";
    public const string ExercisePhaseStarted = "exercise_phase_started"; // memorization, recall
    public const string ExercisePhaseCompleted = "exercise_phase_completed";
    
    // Big Number Exercise
    public const string BigNumberStarted = "big_number_started";
    public const string BigNumberCompleted = "big_number_completed";
    public const string NumberRecallStarted = "number_recall_started";
    public const string NumberRecallCompleted = "number_recall_completed";
    
    // Major System Events
    public const string MajorSystemCategoryViewed = "major_system_category_viewed";
    public const string TokenDetailViewed = "token_detail_viewed";
    public const string DefaultImageChanged = "default_image_changed";
    public const string CustomAssociationAdded = "custom_association_added";
    public const string CustomAssociationDeleted = "custom_association_deleted";
    
    // Pack Management Events
    public const string PackDownloadStarted = "pack_download_started";
    public const string PackDownloaded = "pack_downloaded";
    public const string PackUpdateStarted = "pack_update_started";
    public const string PackUpdated = "pack_updated";
    public const string PackDeleted = "pack_deleted";
    public const string PackSelected = "pack_selected";
    
    // Navigation Events
    public const string ScreenViewed = "screen_view";
    public const string FeatureDiscovered = "feature_discovered";
    public const string ButtonClicked = "button_clicked";
    
    // Training Session Events (Legacy - use FlashNumbers events for new implementations)
    public const string TrainingSessionStarted = "training_session_started";
    public const string TrainingSessionCompleted = "training_session_completed";
    public const string TrainingStepCompleted = "training_step_completed";
    
    // Flash Numbers Events (renamed from Training Session)
    public const string FlashNumbersStarted = "flash_numbers_started";
    public const string FlashNumbersCompleted = "flash_numbers_completed";
    public const string FlashNumbersStepCompleted = "flash_numbers_step_completed";
    
    // Onboarding Events
    public const string OnboardingStarted = "onboarding_started";
    public const string OnboardingCompleted = "onboarding_completed";
    public const string OnboardingStepViewed = "onboarding_step_viewed";
    public const string OnboardingPackDownloadStarted = "onboarding_pack_download_started";
    public const string OnboardingPackDownloadCompleted = "onboarding_pack_download_completed";
    
    // User Engagement Events
    public const string FirstTimeUser = "first_time_user";
    public const string UserRetention = "user_retention";
    public const string DailyActive = "daily_active";
    public const string WeeklyActive = "weekly_active";
    
    // Share Events
    public const string ShareInitiated = "share_initiated";
    public const string ShareCompleted = "share_completed";
    public const string ShareFailed = "share_failed";
}

/// <summary>
/// Standardized parameter names for analytics events
/// </summary>
public static class AnalyticsParameters
{
    // General Parameters
    public const string Platform = "platform";
    public const string AppVersion = "app_version";
    public const string UILanguage = "ui_language";
    public const string FirstLaunch = "first_launch";
    public const string SessionNumber = "session_number";
    public const string DurationSeconds = "duration_seconds";
    public const string Success = "success";
    public const string Attempts = "attempts";
    
    // Exercise Parameters
    public const string ExerciseType = "exercise_type";
    public const string DifficultyDigits = "difficulty_digits";
    public const string NumberRange = "number_range";
    public const string ProgressPercentage = "progress_percentage";
    public const string AbandonmentPoint = "abandonment_point";
    public const string AbandonmentReason = "abandonment_reason";
    public const string PhaseType = "phase_type";
    public const string NumberGenerated = "number_generated";
    public const string UserInput = "user_input"; // Only for debug/development
    public const string MemorizationTime = "memorization_time_seconds";
    public const string RecallTime = "recall_time_seconds";
    
    // Major System Parameters
    public const string Category = "category";
    public const string PackName = "pack_name";
    public const string Token = "token";
    public const string TokensAvailable = "tokens_available";
    public const string TokensMissing = "tokens_missing";
    public const string AvailableAlternatives = "available_alternatives";
    public const string CurrentDefault = "current_default";
    public const string OldDefault = "old_default";
    public const string NewDefault = "new_default";
    public const string OldValue = "old_value";
    public const string NewValue = "new_value";
    
    // Pack Parameters
    public const string PackId = "pack_id";
    public const string PackLanguage = "pack_language";
    public const string DownloadDurationSeconds = "download_duration_seconds";
    public const string PackSizeMB = "pack_size_mb";
    public const string DownloadSuccess = "download_success";
    public const string UpdateAvailable = "update_available";
    
    // Language and Format Parameters
    public const string LanguageCode = "language_code";
    public const string LanguageName = "language_name";
    public const string NumberFormat = "number_format";
    public const string NumberFormatName = "number_format_name";
    
    // Navigation Parameters
    public const string ScreenName = "screen_name";
    public const string CategorySelected = "category_selected";
    public const string FeatureName = "feature_name";
    public const string DiscoveryMethod = "discovery_method";
    public const string ButtonName = "button_name";
    public const string SourceScreen = "source_screen";
    public const string TargetScreen = "target_screen";
    
    // Settings Parameters
    public const string SettingType = "setting_type";
    public const string SettingName = "setting_name";
    
    // Error Parameters
    public const string ErrorMessage = "error_message";
    public const string ErrorType = "error_type";
}

/// <summary>
/// Standardized user property names for analytics segmentation
/// </summary>
public static class AnalyticsUserProperties
{
    public const string PreferredExercise = "preferred_exercise";
    public const string SkillLevel = "skill_level";
    public const string PrimaryLanguage = "primary_language";
    public const string PreferredPack = "preferred_pack";
    public const string MaxDigitsAttempted = "max_digits_attempted";
    public const string ExerciseCompletionRate = "exercise_completion_rate";
    public const string DaysActive = "days_active";
    public const string TotalExercises = "total_exercises";
    public const string FavoriteCategory = "favorite_category";
    public const string AverageSessionDuration = "avg_session_duration";
}

/// <summary>
/// Common values for categories
/// </summary>
public static class Categories
{
    public const string Numbers = "numbers";
    public const string LettersUpper = "letters_upper";
    public const string LettersLower = "letters_lower";
    public const string Symbols = "symbols";
    public const string Months = "months";
}

/// <summary>
/// Common values for platforms
/// </summary>
public static class Platforms
{
    public const string iOS = "iOS";
    public const string Android = "Android";
    public const string Windows = "Windows";
    public const string macOS = "macOS";
}

/// <summary>
/// Common values for skill levels
/// </summary>
public static class SkillLevels
{
    public const string Beginner = "beginner";
    public const string Intermediate = "intermediate";
    public const string Advanced = "advanced";
}

/// <summary>
/// Common values for phase types
/// </summary>
public static class PhaseTypes
{
    public const string Setup = "setup";
    public const string Memorization = "memorization";
    public const string Pause = "pause";
    public const string Recall = "recall";
    public const string Results = "results";
}

/// <summary>
/// Common values for abandonment reasons
/// </summary>
public static class AbandonmentReasons
{
    public const string UserQuit = "user_quit";
    public const string Timeout = "timeout";
    public const string AppBackground = "app_background";
    public const string Error = "error";
    public const string TooHard = "too_hard";
    public const string Distraction = "distraction";
}