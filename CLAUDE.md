# Claude Development Guidelines

This file contains important guidelines and context for Claude when working on this MAUI application.

## Localization Requirements

**CRITICAL**: This application supports both English and Italian languages. Every user-facing string must be localized in both languages.

### When adding new text/strings:

1. **Never hardcode strings** in XAML or code-behind files
2. **Always add to localization resources**:
   - Add English text to: `MemoApp.Localization/Resources/AppResources.resx`
   - Add Italian translation to: `MemoApp.Localization/Resources/AppResources.it.resx`
3. **Use localization markup extension** in XAML: `{localization:Localize YourResourceKey}`
4. **Use localization service** in ViewModels: `_localizationService.GetString("YourResourceKey")`

### Localization Resource Naming Convention:
- Use descriptive keys like: `PageName_ElementPurpose`
- Examples: 
  - `TrainingSession_NextNumber`
  - `MainPage_StartTraining`
  - `Statistics_AverageTime`

### Current Supported Languages:
- **English** (default): `AppResources.resx`
- **Italian**: `AppResources.it.resx`

### Example of proper localization:

**❌ Wrong - Hardcoded:**
```xml
<Label Text="Press N to go to next number" />
```

**✅ Correct - Localized:**
```xml
<Label Text="{localization:Localize TrainingSession_KeyboardShortcutHint}" />
```

**Resource files:**
```xml
<!-- AppResources.resx -->
<data name="TrainingSession_KeyboardShortcutHint" xml:space="preserve">
  <value>Press N to go to next number</value>
  <comment>Keyboard shortcut hint for Next button</comment>
</data>

<!-- AppResources.it.resx -->
<data name="TrainingSession_KeyboardShortcutHint" xml:space="preserve">
  <value>Premi N per andare al numero successivo</value>
  <comment>Keyboard shortcut hint for Next button</comment>
</data>
```

## Platform-Specific Considerations

- **macOS/Windows**: Desktop platforms support keyboard shortcuts
- **iOS/Android**: Mobile platforms may have different UI patterns
- Use `OnPlatform` markup extension when needed for platform-specific behavior

## Architecture Notes

- **Core Logic**: `MemoApp.Core` - Contains game session logic
- **Localization**: `MemoApp.Localization` - Contains all localized resources and services
- **UI**: `MemoApp.UI.MauiApp` - MAUI application with Views and ViewModels
- **MVVM Pattern**: Uses CommunityToolkit.Mvvm for ViewModels and commands

## Testing Requirements

Always test both languages:
1. Test English UI (default)
2. Switch to Italian and verify all text is properly translated
3. Ensure no hardcoded strings remain visible

Remember: **Every user-visible string must be localized in both English and Italian!**