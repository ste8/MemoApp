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
  - `FlashNumbers_NextNumber`
  - `MainPage_BeginFlashNumbers`
  - `Statistics_AverageTime`
  - `Share_FlashNumbersMessage`

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
<data name="FlashNumbers_KeyboardShortcutHint" xml:space="preserve">
  <value>Press N to go to next number</value>
  <comment>Keyboard shortcut hint for Next button</comment>
</data>

<!-- AppResources.it.resx -->
<data name="FlashNumbers_KeyboardShortcutHint" xml:space="preserve">
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
2. **Check branding consistency** - ensure "MemoRaise" is used in user-facing contexts
3. **Add resource keys** to both AppResources.resx and AppResources.it.resx
4. **Plan analytics tracking** - identify user interactions to track
5. **Implement using localization** from the start
6. **Add analytics events** following Firebase conventions
7. **Test in both languages** before considering feature complete
8. **Test analytics events** are properly tracked
9. **Verify social sharing** (if applicable) uses consistent MemoRaise branding
10. **Run MAUI code reviewer** for complex changes to ensure best practices

### Analytics Guidelines:
- Use snake_case for event names (e.g., `exercise_started`)
- Follow Firebase parameter naming conventions
- Track user interactions, not technical operations
- Add analytics to ViewModels, not core services
- Test both Firebase and Local analytics implementations

### Social Sharing Guidelines:
- All sharing templates must use "MemoRaise" branding
- Include proper app download links with consistent naming
- Use descriptive hashtags (#FlashNumbers, #MemoryTraining, #MajorSystem)
- Support both simple messages and detailed templates
- Maintain privacy by sharing metrics, not actual memorized content
- Test sharing functionality across all supported platforms

Remember: **Every user-visible string must be localized in both English and Italian!**

**NEVER** commit code with hardcoded user-facing strings, even temporarily.

**ALWAYS** use "MemoRaise" in user-facing contexts and "Flash Numbers" for training exercises.

## Branding Guidelines

**CRITICAL**: Consistent branding is essential for user trust and app discoverability.

### Commercial vs Internal Names
- **Commercial Name (User-Facing)**: "MemoRaise" - Use in all user-visible contexts
- **Internal Name (Technical)**: "MemoApp" - Use only in code namespaces, project references, and technical contexts

### User-Facing Contexts Requiring "MemoRaise":
- Welcome messages and onboarding
- Social sharing templates and app promotion
- Settings and about pages
- Error messages and notifications
- App store descriptions and metadata
- External communications and links

### Technical Contexts Where "MemoApp" is Acceptable:
- Code namespaces and class names
- Project file names and directory structure
- Internal API endpoints and services
- Developer documentation and logs
- Database schemas and internal identifiers

### Terminology Standardization
- **"Flash Numbers"** - Use instead of generic "training" for number-flash exercises
- **"Big Number"** - Use for number memorization challenges
- **"MemoRaise"** - Commercial brand name in all user communications

## Current Features

- **Multi-language Support**: English/Italian localization with consistent branding
- **Flash Numbers**: Number-to-image association exercises with social sharing and customizable ranges
- **Big Number Challenges**: Timed memory exercises with detailed statistics
- **Social Sharing**: Cross-platform sharing with consistent MemoRaise branding
- **Pack Management**: Downloadable content packs with auto-updates
- **Analytics**: Comprehensive user behavior tracking
- **Cross-platform**: iOS, Android, macOS, Windows support
- **Offline-first**: Works without internet connection
- **Custom Images**: User-generated content with AI assistance
- **Settings Persistence**: Flash Numbers custom range settings saved between sessions

## Recent Architecture Improvements

### Exception Handling Architecture
- **Centralized Exception Handling**: IExceptionHandler service for consistent error management
- **Sentry Integration**: Comprehensive error monitoring with context and categorization
- **No More Silent Failures**: Removed all exception swallowing anti-patterns
- **Localized Error Messages**: User-friendly error messages in both English and Italian

### UI/UX Enhancements
- **Enhanced Visual Hierarchy**: Improved border prominence and component spacing
- **Borderless Native Controls**: Custom handlers for consistent cross-platform appearance
- **Accessibility Compliance**: WCAG-compliant font sizes and contrast ratios
- **Haptic-Free Experience**: Removed all haptic feedback for consistent cross-platform behavior

### iOS App Store Compliance
- **Export Compliance**: Automated encryption declaration in Info.plist
- **ITSAppUsesNonExemptEncryption**: Set to false for standard HTTPS-only encryption

## Git Commit Policy

**CRITICAL**: Never initiate git commits or suggest committing. Focus only on development work.

### Commit Rules:
1. **NEVER commit or suggest committing** - Only commit when the user explicitly asks you to commit
2. **Focus on development only** - Complete the requested work and stop
3. **User controls git workflow** - The user will decide when and what to commit
4. **No commit-related suggestions** - Don't mention committing, branching, or git operations unless specifically asked

This ensures the user maintains complete control over their repository workflow.