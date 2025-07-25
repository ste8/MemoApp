using CommunityToolkit.Mvvm.ComponentModel;

namespace MemoApp.UI.MauiApp.ViewModels;

/// <summary>
/// Base ViewModel that provides common functionality for all ViewModels.
/// Uses CommunityToolkit.Mvvm for property change notifications and commands.
/// </summary>
public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string title = string.Empty;
}