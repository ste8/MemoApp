using MemoApp.Localization.Services;
using Microsoft.Extensions.Logging;

namespace MemoApp.UI.MauiApp.ViewModels.Onboarding;

/// <summary>
/// ViewModel for the Memory Techniques Context view.
/// Introduces the concept that various memory techniques exist and sets context
/// for why MemoRaise focuses on the Major System.
/// </summary>
public partial class MemoryTechniquesContextViewModel : BaseViewModel
{
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<MemoryTechniquesContextViewModel> _logger;

    public MemoryTechniquesContextViewModel(
        ILocalizationService localizationService,
        ILogger<MemoryTechniquesContextViewModel> logger)
    {
        _localizationService = localizationService;
        _logger = logger;

        _logger.LogInformation("Initialized MemoryTechniquesContextViewModel");
    }
}
