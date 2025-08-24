using System.ComponentModel;
using MemoApp.Localization.Services;

namespace MemoApp.Localization.Extensions;

/// <summary>
/// XAML markup extension for localization.
/// Usage: Text="{localization:Localize MainPage_Title}"
/// Usage with args: Text="{localization:Localize TrainingSession_Completed, {Binding CompletedNumbers}}"
/// </summary>
[ContentProperty(nameof(Key))]
public class LocalizeExtension : IMarkupExtension<BindingBase>
{
    public string Key { get; set; } = string.Empty;
    
    public object?[]? Arguments { get; set; }

    public BindingBase ProvideValue(IServiceProvider serviceProvider)
    {
        return new Binding
        {
            Mode = BindingMode.OneWay,
            Path = $"[{Key}]",
            Source = LocalizationResourceManager.Instance
        };
    }

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
    {
        return ProvideValue(serviceProvider);
    }
}

/// <summary>
/// Resource manager for XAML bindings.
/// Provides indexer access to localized strings and handles culture changes.
/// </summary>
public class LocalizationResourceManager : INotifyPropertyChanged
{
    private static LocalizationResourceManager? _instance;
    private ILocalizationService _localizationService;

    public static LocalizationResourceManager Instance
    {
        get
        {
            _instance ??= new LocalizationResourceManager();
            return _instance;
        }
    }

    /// <summary>
    /// Initializes the resource manager with a specific localization service.
    /// This should be called once during app startup with the DI service instance.
    /// </summary>
    public static void Initialize(ILocalizationService localizationService)
    {
        var instance = Instance;
        
        // Unsubscribe from old service if exists
        if (instance._localizationService != null)
        {
            instance._localizationService.CultureChanged -= instance.OnCultureChanged;
        }
        
        // Set new service and subscribe to events
        instance._localizationService = localizationService;
        instance._localizationService.CultureChanged += instance.OnCultureChanged;
        
        // Trigger immediate update for existing bindings
        instance.PropertyChanged?.Invoke(instance, new PropertyChangedEventArgs("Item[]"));
    }

    private LocalizationResourceManager()
    {
        _localizationService = new LocalizationService();
        _localizationService.CultureChanged += OnCultureChanged;
    }

    public string this[string key] => _localizationService.GetString(key);

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnCultureChanged(object? sender, System.Globalization.CultureInfo culture)
    {
        // Ensure UI updates happen on the main thread
        if (Microsoft.Maui.Controls.Application.Current?.Dispatcher != null)
        {
            Microsoft.Maui.Controls.Application.Current.Dispatcher.Dispatch(() =>
            {
                // Notify all bindings that the localized strings have changed
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
                
                // Also notify for indexer changes (alternative approach)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
            });
        }
        else
        {
            // Fallback if no dispatcher available
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }
    }

    public void SetCulture(string cultureName)
    {
        _localizationService.SetCulture(cultureName);
    }

    public void SetCulture(System.Globalization.CultureInfo culture)
    {
        _localizationService.SetCulture(culture);
    }
}