using System;
using System.Threading;
using System.Threading.Tasks;

namespace MemoApp.UI.MauiApp.Services;

/// <summary>
/// Service responsible for coordinating app initialization in proper dependency order
/// </summary>
public interface IAppInitializationService
{
    /// <summary>
    /// Gets whether the app has been fully initialized
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Gets whether critical services have been initialized (app can function)
    /// </summary>
    bool IsCriticalInitialized { get; }

    /// <summary>
    /// Initializes all app services in proper dependency order
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for timeout control</param>
    /// <returns>True if initialization succeeded, false otherwise</returns>
    Task<bool> InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the initialization progress (0.0 to 1.0)
    /// </summary>
    double InitializationProgress { get; }

    /// <summary>
    /// Gets the current initialization status message
    /// </summary>
    string CurrentStatus { get; }
}