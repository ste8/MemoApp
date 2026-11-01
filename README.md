# MemoRaise — Major System Memory Training

**MemoRaise** is a cross-platform app for learning and practicing the **Major System**, a mnemonic technique that turns numbers into words and images you can remember. It's built with .NET MAUI and runs on iOS, Android, macOS (Mac Catalyst) and Windows, in English and Italian.

> *MemoApp* is the internal/technical name used in namespaces and project files. *MemoRaise* is the product name. The project started as a Vibe Coding experiment.

## What is the Major System?

The Major System maps each digit to consonant sounds. You fill in vowels to build words, and the words become vivid images:

| Digit | Sounds | Digit | Sounds |
|---|---|---|---|
| **0** | S, soft C, Z | **5** | L |
| **1** | T, D | **6** | J, SH, CH, soft G |
| **2** | N | **7** | K, hard C, hard G |
| **3** | M | **8** | F, V |
| **4** | R | **9** | P, B |

For example, **42** → R + N → **"Rain"**, which is much easier to picture than two digits.

## Features

- **Onboarding**: a guided introduction to memory techniques and the Major System, with interactive practice, a first challenge, and a choice of number format and image pack.
- **Associations**: browse the word and image for every number from 00 to 99 (or 0 to 99), and replace any of them with your own word and a photo from your gallery.
- **Flash Numbers**: numbers appear in random order and you recall each one's association at your own pace. You can use presets or a custom range, which is saved between sessions. Statistics show your response times, your fastest and slowest numbers, and the ones that need more practice.
- **Big Number**: a timed memorization challenge on a long number, with configurable length, digit grouping, timer and font size. You check the result either inside the app or manually.
- **Image packs**: downloadable association packs, updated automatically in the background.
- **Number formats**: *Padded* (00–99, with 00–09 kept separate from 0–9) or *Natural* (0–99).
- **Sharing**: share Flash Numbers and Big Number results. Only metrics are shared, never your associations.
- **Feedback**: send feedback from inside the app, with optional image attachments.
- **Free and Premium**: ad-supported free use (Google AdMob), with an optional yearly or lifetime Premium subscription through in-app purchase.
- **Offline-first**: everything works without a connection once packs are downloaded.
- **Accessibility**: screen-reader labels and hints, and layouts that adapt to large font sizes.

## Repository layout

```
Repository/
├── src/
│   ├── MemoApp.Core/              # Business logic: Major System, Flash Numbers, Big Number,
│   │                              # packs, associations, settings, analytics/error abstractions
│   ├── MemoApp.Localization/      # RESX resources (en, it) and localization services
│   ├── MemoApp.UI.MauiApp/        # .NET MAUI app: Views, ViewModels, platform services
│   ├── MemoApp.UI.ConsoleApp/     # Minimal console trainer built on MemoApp.Core
│   └── Azure/MemoAppFeedback/     # Azure Function that turns in-app feedback into GitHub issues
├── tests/
│   ├── MemoApp.Core.Tests/        # Unit tests (xUnit)
│   └── MemoApp.Core.IntegrationTests/  # Tests against live external services (OpenAI)
├── docs/                          # Design notes, specs and implementation guides
├── Resources/                     # Repository-level images
└── azure-pipelines.yml            # CI: unit tests, iOS build and TestFlight upload
```

## Tech stack

- **.NET 10** and **.NET MAUI** (Android, iOS, Mac Catalyst, plus Windows when building on Windows)
- **MVVM** with CommunityToolkit.Mvvm and Microsoft.Extensions.DependencyInjection
- **Shell navigation** with five tabs: Home, Associations, Flash Numbers, Big Number, Settings
- **Firebase Analytics** on iOS and Android, with a local analytics fallback on desktop
- **Sentry** for error monitoring
- **Plugin.AdMob** and **Plugin.InAppBilling** for ads and subscriptions
- **xUnit** and **FluentAssertions** for tests

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- The MAUI workload: `dotnet workload install maui`
- For iOS and Mac Catalyst: a Mac with Xcode and its iOS simulators
- For Android: the Android SDK (installed with Visual Studio, or set up separately)

### Build and test

```bash
dotnet build MemoApp.sln
dotnet test tests/MemoApp.Core.Tests/
```

### Run the app

iOS simulator (macOS):

```bash
dotnet build src/MemoApp.UI.MauiApp/MemoApp.UI.MauiApp.csproj -f net10.0-ios -p:RuntimeIdentifier=iossimulator-arm64 -t:Run
```

Mac Catalyst:

```bash
dotnet build src/MemoApp.UI.MauiApp/MemoApp.UI.MauiApp.csproj -f net10.0-maccatalyst -t:Run
```

Android (emulator or connected device):

```bash
dotnet build src/MemoApp.UI.MauiApp/MemoApp.UI.MauiApp.csproj -f net10.0-android -t:Run
```

You can also open `MemoApp.sln` in Visual Studio, Rider or VS Code (with the .NET MAUI extension).

### Console trainer

```bash
dotnet run --project src/MemoApp.UI.ConsoleApp
```

The console app offers quick training on 00–09, custom ranges, a Major System reference, and per-number response-time statistics.

## Configuration

The app reads `src/MemoApp.UI.MauiApp/appsettings.json`, with `appsettings.Production.json` overriding it in release builds.

| Setting | Purpose |
|---|---|
| `PackManagement:PacksIndexUrl` | Where the pack index is downloaded from |
| `Sentry:Dsn` | Sentry project that receives error reports. Leave it empty to disable Sentry. |
| `Freemium:AdMob:*` | AdMob ad unit IDs. The defaults in `appsettings.json` are Google's test IDs. |
| `Feedback:ApiUrl`, `Feedback:ApiKey` | Feedback endpoint and key (see [feedback-config-example.json](feedback-config-example.json)) |

Firebase configuration lives in `Platforms/Android/google-services.json` and `Platforms/iOS/GoogleService-Info.plist`.

**Integration tests** need an OpenAI API key, supplied through user secrets or the `OpenAI__ApiKey` environment variable. See [tests/MemoApp.Core.IntegrationTests/README.md](tests/MemoApp.Core.IntegrationTests/README.md). CI doesn't run them.

**Feedback function**: `src/Azure/MemoAppFeedback` reads `GITHUB_TOKEN`, `GITHUB_OWNER`, `GITHUB_REPO` and `FEEDBACK_API_KEY` from its application settings.

## Localization

Every user-visible string lives in `src/MemoApp.Localization/Resources/AppResources.resx` (English) and `AppResources.it.resx` (Italian). XAML uses `{localization:Localize Key}`, and ViewModels use `ILocalizationService.GetString("Key")`. Keys follow the `PageName_ElementPurpose` pattern (for example `FlashNumbers_NextNumber`). No hardcoded UI strings are allowed. [CLAUDE.md](CLAUDE.md) has the full rules.

## Continuous integration

[azure-pipelines.yml](azure-pipelines.yml) runs the unit tests on `main`, `feat/*` and `build/iOS`. Signing, packaging and the TestFlight upload run only on `build/iOS`. Signing certificates, provisioning profiles and passwords are stored in Azure DevOps secure files and variable groups, not in this repository.

## Documentation

- [docs/ERROR_MONITORING.md](docs/ERROR_MONITORING.md): Sentry integration and exception handling
- [docs/ShareFeatureImplementation.md](docs/ShareFeatureImplementation.md): social sharing
- [docs/PACK_SELECTOR_MIGRATION_GUIDE.md](docs/PACK_SELECTOR_MIGRATION_GUIDE.md): the pack selection system
- [docs/UI_DESIGN_RECOMMENDATIONS.md](docs/UI_DESIGN_RECOMMENDATIONS.md): UI guidelines
- [docs/specs/](docs/specs/): audits and improvement plans
- [project-context.md](project-context.md): architecture overview and development history
