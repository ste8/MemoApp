# Claude Development Guidelines

This file contains important guidelines and context for Claude when working on this MAUI application.

## Localization Requirements

**CRITICAL**: This application supports both English and Italian languages. Every user-facing string must be localized in both languages.

### MANDATORY RULE: NO HARDCODED STRINGS

**ANY** text that users can see MUST be localized. This includes:
- Page titles, headers, labels, buttons
- Error messages, tooltips, placeholders
- Tab names, menu items, section titles
- Default/fallback values shown in UI
- Even temporary or debug strings visible to users

### When adding new text/strings:

1. **Never hardcode strings** in XAML, code-behind files, or ViewModels
2. **Always add to localization resources FIRST**:
   - Add English text to: `src/MemoApp.Localization/Resources/AppResources.resx`
   - Add Italian translation to: `src/MemoApp.Localization/Resources/AppResources.it.resx`
3. **Use localization markup extension** in XAML: `{localization:Localize YourResourceKey}`
4. **Use localization service** in ViewModels: `_localizationService.GetString("YourResourceKey")`
5. **Include comments** in resource files explaining context for translators

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

## Project Structure

The project follows .NET ecosystem best practices with clear separation:

```
Repository/
├── src/                           # Source code
│   ├── MemoApp.Core/             # Core business logic & analytics
│   ├── MemoApp.Localization/     # Localization resources & services
│   ├── MemoApp.UI.MauiApp/       # MAUI application (Views, ViewModels, Services)
│   └── MemoApp.UI.ConsoleApp/    # Console application
├── tests/                         # Test projects
│   ├── MemoApp.Core.Tests/       # Unit tests
│   └── MemoApp.Core.IntegrationTests/ # Integration tests (OpenAI, etc.)
├── docs/                         # Documentation
└── Resources/                    # Repository-level resources
```

## Architecture Notes

- **Core Logic**: `src/MemoApp.Core` - Contains game session logic, analytics, pack management
- **Localization**: `src/MemoApp.Localization` - Contains all localized resources and services
- **UI**: `src/MemoApp.UI.MauiApp` - MAUI application with Views, ViewModels, and platform services
- **MVVM Pattern**: Uses CommunityToolkit.Mvvm for ViewModels and commands
- **Analytics**: Comprehensive Firebase Analytics integration with local fallback
- **Pack System**: Downloadable content packs with automatic updates
- **Testing**: Unit tests and integration tests for external services

## Testing Requirements

### Language Testing:
1. Test English UI (default)
2. Switch to Italian and verify all text is properly translated
3. Ensure no hardcoded strings remain visible
4. Check that new features work correctly in both languages
5. Verify fallback strings are also localized

### Build & Test Commands:
- **Build**: `dotnet build MemoApp.sln` from repository root
- **Unit Tests**: `dotnet test tests/MemoApp.Core.Tests/`
- **Integration Tests**: `dotnet test tests/MemoApp.Core.IntegrationTests/`
- **All Tests**: `dotnet test` from repository root

### Analytics Testing:
- Verify analytics events are triggered correctly
- Test both Firebase (iOS/Android) and Local (Windows/macOS) implementations
- Check analytics parameters follow Firebase naming conventions

## Development Workflow for New Features

When adding any new UI feature:
1. **Plan localization first** - identify all text that will be visible
2. **Add resource keys** to both AppResources.resx and AppResources.it.resx
3. **Plan analytics tracking** - identify user interactions to track
4. **Implement using localization** from the start
5. **Add analytics events** following Firebase conventions
6. **Test in both languages** before considering feature complete
7. **Test analytics events** are properly tracked

### Analytics Guidelines:
- Use snake_case for event names (e.g., `exercise_started`)
- Follow Firebase parameter naming conventions
- Track user interactions, not technical operations
- Add analytics to ViewModels, not core services
- Test both Firebase and Local analytics implementations

Remember: **Every user-visible string must be localized in both English and Italian!**

**NEVER** commit code with hardcoded user-facing strings, even temporarily.

## Current Features

- **Multi-language Support**: English/Italian localization
- **Major System Training**: Number-to-image association exercises
- **Number Memorization**: Timed memory exercises with statistics
- **Training Sessions**: Structured practice with progress tracking
- **Pack Management**: Downloadable content packs with auto-updates
- **Analytics**: Comprehensive user behavior tracking
- **Cross-platform**: iOS, Android, macOS, Windows support
- **Offline-first**: Works without internet connection
- **Custom Images**: User-generated content with AI assistance

## Git Commit Policy

**CRITICAL**: Never initiate git commits or suggest committing. Focus only on development work.

### Commit Rules:
1. **NEVER commit or suggest committing** - Only commit when the user explicitly asks you to commit
2. **Focus on development only** - Complete the requested work and stop
3. **User controls git workflow** - The user will decide when and what to commit
4. **No commit-related suggestions** - Don't mention committing, branching, or git operations unless specifically asked

This ensures the user maintains complete control over their repository workflow.