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
    private readonly ILocalizationService _localizationService;

    public static LocalizationResourceManager Instance
    {
        get
        {
            _instance ??= new LocalizationResourceManager();
            return _instance;
        }
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
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
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