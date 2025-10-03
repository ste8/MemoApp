namespace MemoApp.UI.MauiApp.ViewModels.Onboarding;

/// <summary>
/// Configuration constants for the two-phase onboarding system.
/// Phase 1: Tutorial (no skip allowed) - positions 0-4
/// Phase 2: Setup (skip allowed) - positions 5-7
/// </summary>
public static class OnboardingPhaseConfig
{
    /// <summary>
    /// The last position index for Phase 1 (Tutorial phase).
    /// Positions 0 through this value are considered Phase 1.
    /// </summary>
    public const int TUTORIAL_PHASE_LAST_POSITION = 4;

    /// <summary>
    /// Number of steps in Phase 1 (Tutorial).
    /// </summary>
    public const int TUTORIAL_PHASE_STEP_COUNT = 5;

    /// <summary>
    /// Number of steps in Phase 2 (Setup).
    /// </summary>
    public const int SETUP_PHASE_STEP_COUNT = 3;

    /// <summary>
    /// Total number of onboarding steps across both phases.
    /// </summary>
    public const int TOTAL_STEP_COUNT = TUTORIAL_PHASE_STEP_COUNT + SETUP_PHASE_STEP_COUNT;

    /// <summary>
    /// Determines if the given position is in the Tutorial phase (Phase 1).
    /// </summary>
    /// <param name="position">The current onboarding position (0-based index)</param>
    /// <returns>True if the position is in Phase 1 (Tutorial), false otherwise</returns>
    public static bool IsInTutorialPhase(int position) => position <= TUTORIAL_PHASE_LAST_POSITION;

    /// <summary>
    /// Determines if the given position is in the Setup phase (Phase 2).
    /// </summary>
    /// <param name="position">The current onboarding position (0-based index)</param>
    /// <returns>True if the position is in Phase 2 (Setup), false otherwise</returns>
    public static bool IsInSetupPhase(int position) => position > TUTORIAL_PHASE_LAST_POSITION;

    /// <summary>
    /// Gets the phase number (1 or 2) for the given position.
    /// </summary>
    /// <param name="position">The current onboarding position (0-based index)</param>
    /// <returns>1 for Tutorial phase, 2 for Setup phase</returns>
    public static int GetPhaseNumber(int position) => IsInTutorialPhase(position) ? 1 : 2;

    /// <summary>
    /// Gets the step number within the current phase for the given position.
    /// </summary>
    /// <param name="position">The current onboarding position (0-based index)</param>
    /// <returns>1-based step number within the current phase</returns>
    public static int GetPhaseStepNumber(int position)
    {
        return IsInTutorialPhase(position)
            ? position + 1  // Phase 1: positions 0-3 become steps 1-4
            : position - TUTORIAL_PHASE_LAST_POSITION;  // Phase 2: positions 4-6 become steps 1-3
    }
}