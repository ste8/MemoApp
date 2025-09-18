using MemoApp.UI.MauiApp.Handlers;

namespace MemoApp.UI.MauiApp.Configuration;

/// <summary>
/// Handles MAUI-specific application configuration including fonts, platform settings, and core MAUI setup
/// </summary>
public static class MauiConfiguration
{
    /// <summary>
    /// Configures the core MAUI application settings, fonts, and platform-specific features
    /// </summary>
    /// <param name="builder">The MAUI application builder</param>
    /// <returns>The configured builder for method chaining</returns>
    public static MauiAppBuilder ConfigureMauiApp(this MauiAppBuilder builder)
    {
        return builder
            .UseMauiApp<App>()
            .ConfigureFonts(ConfigureFonts)
            .ConfigureMauiHandlers(handlers =>
            {
                // Configure borderless Entry and Editor controls for feedback forms
                BorderlessEntryHandler.Configure();
                BorderlessEditorHandler.Configure();

                // Configure Editor to support numeric keyboard on mobile platforms
                Microsoft.Maui.Handlers.EditorHandler.Mapper.AppendToMapping("NumericKeyboard", (handler, view) =>
                {
                    if (view is Editor editor && editor.Keyboard == Keyboard.Numeric)
                    {
#if IOS || MACCATALYST
                        handler.PlatformView.KeyboardType = UIKit.UIKeyboardType.NumberPad;
#elif ANDROID
                        handler.PlatformView.InputType = Android.Text.InputTypes.ClassNumber;
#endif
                    }
                });
            });
    }

    /// <summary>
    /// Configures fonts for the MAUI application
    /// </summary>
    /// <param name="fonts">The font collection to configure</param>
    private static void ConfigureFonts(IFontCollection fonts)
    {
        // System fonts
        fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
        fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
        
        // FontAwesome v6 Pro fonts for icons
        fonts.AddFont("fa-solid-900.ttf", "FontAwesomeSolid");
        fonts.AddFont("fa-regular-400.ttf", "FontAwesomeRegular");
        fonts.AddFont("fa-light-300.ttf", "FontAwesomeLight");
        fonts.AddFont("fa-thin-100.ttf", "FontAwesomeThin");
        fonts.AddFont("fa-duotone-900.ttf", "FontAwesomeDuotone");
        fonts.AddFont("fa-brands-400.ttf", "FontAwesomeBrands");
    }
}