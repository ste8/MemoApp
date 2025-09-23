using MemoApp.Core.MajorSystem.Models;

namespace MemoApp.UI.MauiApp.Views.Onboarding;

public partial class MajorSystemIntroView : ContentView
{
    private readonly IDigitLetterMappingService _mappingService;

    public MajorSystemIntroView(IDigitLetterMappingService mappingService)
    {
        _mappingService = mappingService;
        InitializeComponent();
    }

    protected override void OnParentSet()
    {
        base.OnParentSet();

        // Initialize the compact mapping view with the service
        if (Parent != null && CompactMappingView != null)
        {
            CompactMappingView.MappingService = _mappingService;
        }
    }
}