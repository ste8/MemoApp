namespace MemoApp.UI.MauiApp.Services.Analytics;

/// <summary>
/// Carries a Firebase initialisation failure from platform startup to the point where
/// error monitoring exists.
///
/// Firebase is initialised in <c>MainApplication.OnCreate</c> (Android) and
/// <c>AppDelegate.FinishedLaunching</c> (iOS), both of which run before dependency
/// injection is available, so a failure there cannot be reported at the point it happens.
/// Recording it here means it still reaches error monitoring during app initialisation
/// instead of being lost - a silent failure would leave analytics dead across an entire
/// platform with nothing to indicate why.
/// </summary>
public static class FirebaseInitializationStatus
{
    private static Exception? _failure;

    public static bool HasFailed => _failure is not null;

    public static void RecordFailure(Exception exception) => _failure = exception;

    /// <summary>
    /// Returns the recorded failure and clears it, so it is reported once per launch.
    /// </summary>
    public static Exception? Consume()
    {
        var failure = _failure;
        _failure = null;
        return failure;
    }
}
