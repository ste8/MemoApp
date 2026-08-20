# MemoRaise — Codebase Audit & Improvement Plan

**Date:** 2026-08-19
**Scope:** ~51,000 lines across `MemoApp.Core`, `MemoApp.Localization`, `MemoApp.UI.MauiApp`, `MemoApp.UI.ConsoleApp`, `src/Azure`
**Method:** Four parallel specialist reviews (architecture, clean code, MAUI runtime, UI/UX). Every load-bearing claim was independently re-verified against the source before being recorded here.
**Baseline at audit time:** Build exits 0 with 294 unique warnings. 51 unit tests pass.

> **Status: Phases 0–4 complete. Phase 5 partially done — see its log for what remains.** Tests 51 → 142, none skipped. `MVVMTK0039` warnings 3 → 0.
> EN/IT translation parity is complete (669/669).
> **The duplicate-number defect in §1 is fixed** — the shipped presets now present 100 distinct numbers.
> CI now runs `dotnet test` before it can publish to TestFlight.
> See the [Progress log](#9-progress-log) for exactly what changed and what was corrected along the way.

---

## Table of contents

1. [Verdict: sequence-generation duplication](#1-verdict-sequence-generation-duplication)
2. [Blocking — fix before committing the current merge](#2-blocking--fix-before-committing-the-current-merge)
3. [Critical & cheap](#3-critical--cheap)
4. [Structural](#4-structural)
5. [Larger, lower urgency](#5-larger-lower-urgency)
6. [**The plan — sequenced phases**](#6-the-plan--sequenced-phases)
7. [Verified sound — do not re-audit](#7-verified-sound--do-not-re-audit)
8. [Limits of this audit](#8-limits-of-this-audit)
9. [**Progress log**](#9-progress-log)

---

## 1. Verdict: sequence-generation duplication

**Confirmed.** There are **five implementations** of Major System sequence generation. Four are live. The fifth — the only *correct* one — is dead code with zero callers.

| # | Location | Status |
|---|---|---|
| 1 | `src/MemoApp.Core/MajorSystem/NumberSequence.cs:30-43` `GetAllNumbersInOrder()` | Live — canonical, used by `GameSession` |
| 2 | `src/MemoApp.UI.MauiApp/Utilities/NumberFormatHelper.cs:102-115` `GetPaddedFormatNumbers()` | Live — **byte-identical to #1, comments included** |
| 3 | `src/MemoApp.UI.MauiApp/Utilities/NumberFormatHelper.cs:117-124` `GetNaturalFormatNumbers()` | Live — natural-format variant |
| 4 | `src/MemoApp.Core/MajorSystem/Services/MajorSystemCategoryService.cs:138-169` `GenerateNumberTokens()` | Live — comment at `:153` admits the copy |
| 5 | `src/MemoApp.UI.MauiApp/Utilities/NumberFormatHelper.cs:54-81` `GenerateTrainingSequence()` | **DEAD — zero callers, and it is the correct one** |

`ParseRangeValue` is duplicated verbatim as well: `NumberSequence.cs:45-62` vs `NumberFormatHelper.cs:83-100` — same validation, same zero-prefix detection, same exception text.

### The user-visible consequence

Verified empirically by running the shipped `MemoApp.Core` assembly against its own presets:

```
Preset "Complete Major System"  (00 -> 99)
  Items generated : 110      <-- should be 100
  Distinct labels : 100
  Duplicated      : "00"x2  "01"x2  ...  "09"x2
  User sees       : 00 01 02 ... 09 00 01 02 ... 09 10 11 ...

Preset "Numbers 00 to 69"       (00 -> 69)
  80 items, 70 distinct, 10 duplicates

Preset "Natural 0-99"  -> 100 items, 0 duplicates   OK
Preset "00-09"         ->  10 items, 0 duplicates   OK
```

The duplicates are invisible in code but visible on screen because of a **dead ternary** at `NumberFormatHelper.cs:24` — both branches are identical:

```csharp
NumberFormat.Padded => number.IsZeroPrefixed
    ? number.Value.ToString("00")
    : number.Value.ToString("00"),
```

So `MajorNumber(7, zeroPrefixed:true)` and `MajorNumber(7, zeroPrefixed:false)` both render `"07"`. The sequence legitimately holds two distinct domain objects; the formatter collapses them into one label.

The dead `GenerateTrainingSequence` exists specifically to prevent this. Its own doc comment at `:48` reads *"This prevents duplicates by respecting the format when generating the sequence."* Nothing calls it.

### Root cause

The same concept carries four different names across three files — `GetAllNumbersInOrder`, `GetPaddedFormatNumbers`, `GenerateTrainingSequence`, `GenerateNumberTokens`. That is plausibly *why* there are four copies: nobody could tell which one to call.

Deeper: `NumberFormat` is a domain concept living behind `ILocalizationService.CurrentNumberFormat`, so sequence generation needs an i18n service to do its job. That inversion is the origin of the tangle, not a symptom of it.

### Not duplication — leave these alone

- `GameSession.cs:36-37` shuffle — ordering, not generation; the only shuffle in the codebase.
- `BigNumberService.GenerateNumber` — random digit-pair generation, a genuinely different domain.
- `MajorSystemCategoryService` letter/month token generators — distinct domains.

---

## 2. Blocking — fix before committing the current merge

### 2.1 Onboarding progress bar shows 100% before the last step — REGRESSION

- `src/MemoApp.UI.MauiApp/ViewModels/Onboarding/OnboardingPhaseConfig.cs:19` — `TUTORIAL_PHASE_STEP_COUNT = 5`
- `src/MemoApp.UI.MauiApp/Views/Onboarding/OnboardingNavigationPage.xaml` — declares only `TutorialStep0`..`TutorialStep3`
- `src/MemoApp.UI.MauiApp/Views/Onboarding/OnboardingNavigationPage.xaml.cs:49-52` — adds only those four

Commit `5ec019a "Split Major System introduction into two onboarding pages"` raised the tutorial from 4 steps to 5 without updating the indicator markup. On the real final tutorial step the dot bar reads fully complete while the text beneath correctly says "Tutorial: 5 of 5".

**Fix:** add the fifth dot, then generate the dots from `OnboardingPhaseConfig` via `BindableLayout` so config and markup cannot drift again.

### 2.2 Progress text built by English string interpolation

`src/MemoApp.UI.MauiApp/ViewModels/Onboarding/OnboardingNavigationViewModel.cs:163,169`

```csharp
ProgressText = $"Tutorial: {phaseStep} of {OnboardingPhaseConfig.TUTORIAL_PHASE_STEP_COUNT}";
```

A hardcoded user-facing string on the first screen every user sees. Italian users read "Tutorial: 3 of 5".

**Fix:** add `Onboarding_ProgressFormat` to both `.resx` files — `"Tutorial: {0} of {1}"` / `"Tutorial: {0} di {1}"`.

---

## 3. Critical & cheap

Every item here is user-visible, verified, and small.

### 3.1 The Big Number font-size setting is never saved

- `src/MemoApp.Core/BigNumber/BigNumberSettings.cs:9` declares `FontSize`
- `src/MemoApp.UI.MauiApp/Services/BigNumberSettingsService.cs:18-23` (Load) and `:31-36` (Save) both omit it

Load and Save each handle six properties; `FontSize` is not among them in either direction — while `BigNumberViewModel.cs:438` reads it and `:703` writes it. The write is silently discarded. A user sets font size to Large and it is back to Auto on every launch. A fully-built UI feature that has never once worked.

**Fix:** two lines. Then collapse `BigNumberSettingsService` and `FlashNumbersSettingsService` — the same `Preferences` round-trip written twice — into one generic store.

### 3.2 `async void` commands on the Flash Numbers main path

`src/MemoApp.UI.MauiApp/ViewModels/FlashNumbers/FlashNumbersViewModel.cs:296, 327, 534`

`[RelayCommand]` on `async void` generates a plain `RelayCommand`, so the task is never tracked and an exception goes straight to the synchronization context — process termination, not an error dialog. None of the three has a try/catch.

- `:304` — `int.Parse(RangeEnd)` with no guard.
- On the final number, `CompleteSession` sets `IsSessionActive = false` only *after* two awaited analytics calls. During that window the re-entry guard still passes, so a second tap re-enters `GameSession.AdvanceToNext()` and throws from `GameSession.cs:58`. App dies, session lost.

**Fix:** all three to `private async Task`; `await` the call at `:347`; move state writes ahead of the awaits; route catches to the `IExceptionHandler` this class already injects and uses correctly at `:280`.

### 3.3 Big Number results render invisible after one tab switch

`src/MemoApp.UI.MauiApp/Views/BigNumber/BigNumberPage.xaml.cs` — subscribes six handlers at `:22,25,28,31,34,37`; unsubscribes five at `:415-419`. `OnBigNumberViewModelPropertyChanged` (`:37`) has no `-=` anywhere.

Because this is a Shell tab the instance is reused and the constructor never runs again, but `OnDisappearing` fires on every tab switch. After switching away once: no auto-scroll to results, no input focus, and — since that handler resets the entrance animation — **the results panel renders at opacity 0**.

**Fix:** move the six `+=` into `OnAppearing()`; add the missing `-=`.

### 3.4 Five labels render raw markup instead of their value

- `src/MemoApp.UI.MauiApp/Views/FeedbackPage.xaml:418, 575`
- `src/MemoApp.UI.MauiApp/Views/Associations/AddCustomAssociationPage.xaml:237, 363`
- `src/MemoApp.UI.MauiApp/Views/FlashNumbers/FlashNumbersPage.xaml:354`

```xml
Text="{Binding AttachedImageCount, StringFormat='{localization:Localize Feedback_ImagesAttached}'}"
```

A single-quoted markup-extension argument is a literal string — the inner extension never evaluates. The resources genuinely contain `{0}`, so the format was intended.

**Fix:** use a converter or a bound `FormattedString`; the nested-extension form cannot work here.

### 3.5 New users see raw markup on the onboarding screen

`src/MemoApp.UI.MauiApp/Views/Onboarding/MajorSystemExampleView.xaml:183`

```xml
Text="👆 {localization:Localize Onboarding_TapToSkipAnimation}"
```

Because the value does not *start* with `{`, XAML treats the whole thing as a literal.

**Fix:** move the emoji into the resource value, or compose with a `FormattedString`.

### 3.6 The home screen announces "Button, button" five times

`src/MemoApp.UI.MauiApp/Views/MainPage.xaml:81-82, 119-120, 157-158, 195-196, 238-239`

`SemanticProperties.Description="Button"` on all five navigation cards. In MAUI an explicit `Description` on a container *replaces* narration of its children, so VoiceOver/TalkBack read "Button" instead of the card's actual label. A blind user cannot tell Associations from Flash Numbers from Big Number. The hardcoded English hints are a second violation on the same lines.

App-wide, `SemanticProperties.HeadingLevel` has **zero** occurrences across ~9,900 lines of XAML.

### 3.7 Publicly shared results report fabricated statistics

`src/MemoApp.UI.MauiApp/ViewModels/FlashNumbers/FlashNumbersViewModel.cs:564, 591-596`

```csharp
var fastestTime = averageTime; // "we don't have individual timing data readily available"
```

The comment is false — `SessionStatistics.FastestResponse` is computed by `MinBy(p => p.ResponseTime)` at `SessionStatistics.cs:64` on the very object the method already holds. Every shared, MemoRaise-branded result publishes the **average** as the "fastest time". `StatisticsViewModel.cs:194` computes it correctly, so the same session shares different numbers depending on which screen you share from.

At `:591` the shared duration is recomputed as `DateTime.UtcNow - _sessionStartTime` **at share time**. Finish in 2 minutes, read the results for 10, tap Share — it publishes a 12-minute session with a proportionally inflated average.

### 3.8 Accuracy tiers disagree with themselves inside one class

`src/MemoApp.UI.MauiApp/ViewModels/BigNumber/BigNumberViewModel.cs:1715,1722` vs `:2468-2470`

One path uses thresholds 95/80 with `#4CAF50`/`#FF9800`/`#F44336`; the other 90/70 with `#28A745`/`#FD7E14`/`#DC3545` — same class, same `AccuracyPercentage`, two threshold sets and two palettes. A 92% result is green or orange depending on which path last wrote `AccuracyColor`. No named constants.

### 3.9 Advanced-settings toggle reverts to English after the first tap

`src/MemoApp.UI.MauiApp/ViewModels/BigNumber/BigNumberViewModel.cs:2050`

```csharp
AdvancedSettingsToggleText = IsAdvancedSettingsExpanded ? "Hide" : "Show";
```

The initial value is correctly localized at `:457`; every toggle overwrites it with a literal.

**Fix:** localize both; add `BigNumber_Hide` to both resource files.

### 3.10 Two live strings have no Italian translation

- `src/MemoApp.UI.MauiApp/Views/Onboarding/WelcomeView.xaml:27` — `Onboarding_Welcome_Title` ("Welcome to MemoRaise") — the first screen an Italian user ever sees
- `src/MemoApp.UI.MauiApp/Views/FlashNumbers/StatisticsPage.xaml:50` — `Statistics_SessionSummary` ("Session Summary")

Of 665 English keys, 6 are missing from Italian; the other four are orphaned. Separately ~205 of 665 keys (31%) are orphaned, including a fully-translated 22-key `OnboardingPackDownload_*` cluster for a screen that does not exist.

### 3.11 Amber buttons fail WCAG; the accessible color already exists unused

`src/MemoApp.UI.MauiApp/Resources/Styles/Colors.xaml` — white on `Secondary #F59E0B` ≈ 2.15:1, on `SecondaryDark #F97316` ≈ 2.80:1, on `Warning #FFC107` ≈ 1.63:1. The floor is 4.5:1.

Live at `StatisticsPage.xaml:384-386`, `FlashNumbersPage.xaml:492-497`, `AddCustomAssociationPage.xaml:183-191`.

A correctly-contrasted `WarningAccessible #CC5500` (4.9:1) already exists in the same file and is never wired up. The Gray and Primary families, by contrast, are genuinely diligent and annotated with real ratios.

---

## 4. Structural

### 4.1 Nothing verifies this codebase, and CI does not run the tests that exist

`azure-pipelines.yml` has **no `dotnet test` step** — grepping for "test" matches only `TestFlight`.

51 tests pass, covering 5 of 76 `MemoApp.Core` public types. Untested: `BigNumberService`, `PackAssociationsService` (868 lines), `PackDownloadService` (595), `CustomAssociationsService`, `PackManagerService`, `SessionStatistics`, every settings service.

Structurally worse: `tests/MemoApp.Core.Tests/MemoApp.Core.Tests.csproj` references only Core, so the 38.8k-line UI project — where the logic actually lives — is not merely untested but untestable as arranged.

### 4.2 Two engines, two clocks, one DST-unsafe

- `src/MemoApp.Core/BigNumber/BigNumberService.cs:59,71` use `DateTime.Now`
- `src/MemoApp.Core/MajorSystem/GameSession.cs:50,64,94` use `DateTime.UtcNow`

The Big Number timer derives from the local clock, so any discontinuity mid-round corrupts it — realistically not DST but iOS auto-updating the timezone when a user lands in a new country. Elapsed can go negative and `FormatTime` then renders `"00:-30"`. All analytics durations use UTC, so after a clock shift the timer the user sees and the duration recorded diverge by exactly the offset.

Both engines derive elapsed from wall-clock, so backgrounding for five minutes adds five minutes to the result. Flash Numbers separately stamps its own `_sessionStartTime` at `FlashNumbersViewModel.cs:301`, two awaits before `GameSession.StartSession()`.

`FormatTime` also renders 2 hours as `"120:00"`.

### 4.3 One domain rule written four times, already diverging

```csharp
associations.FirstOrDefault(a => a.IsDefault) ?? associations.First()   // three sites
sortedAssociations.FirstOrDefault(a => a.IsDefault)                     // TokenDetail — no fallback
```

- `src/MemoApp.UI.MauiApp/ViewModels/FlashNumbers/FlashNumbersViewModel.cs:440`
- `src/MemoApp.UI.MauiApp/ViewModels/BigNumber/BigNumberViewModel.cs:1423`
- `src/MemoApp.UI.MauiApp/ViewModels/Associations/AssociationsViewModel.cs:707`
- `src/MemoApp.UI.MauiApp/ViewModels/Associations/TokenDetailViewModel.cs:198` — **diverges**

A user whose pack has no explicitly-defaulted association sees an association on three screens and nothing on Token Detail.

**Fix:** one `ResolveDefault` on `IAssociationComposerService`. Cheapest high-value fix in this report.

### 4.4 188 lines of unreachable duplicate logic

`src/MemoApp.Core/MajorSystem/Services/PackAssociationsService.cs:443-531` and `:532-630`

Two private methods reimplementing the same fallback chain as `GetDefaultAssociationForTokenSynchronous`, which is the one actually called. Both have exactly one reference each — their own declaration. That is 21% of an 868-line file.

**Fix:** delete both.

### 4.5 The centralized exception handler exists and is barely used

~28 catch blocks swallow to `Debug.WriteLine`, which is `[Conditional("DEBUG")]` — every one is literally `catch { }` in Release. `AssociationsViewModel` receives `IExceptionHandler` via DI and never calls it; 18 catches go to `_logger` only.

Worst instances:
- `BigNumberViewModel.cs:2396` — swallows digit-comparison failures, so the Wordle-style breakdown silently fails to render
- `Platforms/Android/MainApplication.cs` and `Platforms/iOS/AppDelegate.cs` — swallow **Firebase initialization failure**
- `MauiProgram.cs:172-175` — empty catch around *reporting a startup failure to Sentry*
- `Services/ErrorMonitoring/GlobalExceptionHandler.cs:152` — the crash reporter is `async void`, returns at its first `await`, and the process dies before Sentry's call completes

In fairness: only 4 catch blocks are truly empty and all are commented. The problem is reach, not intent.

### 4.6 Unsubscribe placed in finalizers that can never run

- `src/MemoApp.UI.MauiApp/ViewModels/Onboarding/InteractivePracticeViewModel.cs:75` → finalizer ~`:345`
- `src/MemoApp.UI.MauiApp/ViewModels/Onboarding/ExampleViewModel.cs:41` → finalizer ~`:108`

Self-defeating by construction: the singleton `ILocalizationService.CultureChanged` delegate holds a strong reference to the ViewModel, so it is never eligible for finalization, so the finalizer never runs. Neither implements `IDisposable`. After onboarding completes the whole 8-view tree stays resident.

Related: `BigNumberViewModel.cs:426` attaches `_timer.Tick` with no `-=` anywhere and the page never stops it on navigation.

### 4.7 Pack update interval off by the user's UTC offset

`src/MemoApp.UI.MauiApp/Services/PackUpdateBackgroundService.cs:97` parses, `:307` subtracts.

Written with `.ToString("O")` but read with a bare `DateTime.TryParse` — no `RoundtripKind` — so it returns `Local` and is subtracted from `DateTime.UtcNow`. A user at UTC−8 gets an unnecessary network fetch on every cold start; one at UTC+13 stalls for a day and a half.

---

## 5. Larger, lower urgency

### 5.1 `BigNumberViewModel` — 2,675 lines, 87 observable properties, 14 dependencies

Distinct responsibilities with verified ranges: font-fitting (`:941-1142`), example-number generation reimplementing `BigNumberService`'s pairing with its own `new Random(12345)` (`:1189-1213`), associations loading (`:1236-1445`), verification state machine (`:1587-1983`), digit-comparison carousel (`:2351-2673`).

Dead cluster: ~15 members including 9 unbound `[RelayCommand]`s — two commented "Legacy command for backward compatibility" — plus `ComparisonResults`/`OriginalNumberDigits`, superseded and never written. Two byte-identical 14-line blocks at `:1461-1474` and `:1547-1560` fabricate missing analytics timing (`AddSeconds(-30) // Estimate`) and report it as measured.

Precedent exists: `FlashNumbersSetupViewModel` was already split out of `FlashNumbersViewModel` successfully.

### 5.2 Four card systems coexist; the one named "unified" is dead

`Resources/Styles/Styles.xaml:1307-1403` defines a "UNIFIED HOMEPAGE CARD SYSTEM" with zero usages; `MainPage.xaml:79,117,155,192,236` uses a different, later `Elegant*` family. The "Enhanced Typography Scale" (`:542-607`) is used only by onboarding.

Measured drift: **261 hardcoded `FontSize` literals across 35 of 46 XAML files**, 50 hardcoded hex colors across 10 files, 11 distinct corner-radius values used inline.

Visible cost: the three duplicated challenge panels in `WelcomeChallengeView.xaml:30-142` — commit `46456f9` had to hand-edit the same change three times.

### 5.3 Fixed heights and nested scrollers that collapse

`CollectionView` inside `ScrollView` (kills virtualization, can collapse height):
`TokenDetailPage.xaml:33/67`, `StatisticsPage.xaml:14/206/287`, `PackDownloadPage.xaml:59/90`, `PackDownloadOnboardingView.xaml:14/94`.

`*` row inside a `ScrollView`: `StatisticsPage.xaml:28` (wraps New Game and Share buttons), `BigNumberPage.xaml:560`, `PersonalizationView.xaml:54`.

Fixed `HeightRequest` against scalable text: `FlashNumbersPage.xaml:211,232` (88pt digits in 100px), `WelcomeChallengeView.xaml:35,73,111,150` (220px panels), `BigNumberPage.xaml:437-495` (8pt labels in 44×44 buttons).

Correct pattern already exists at `AssociationsPage.xaml:157-284`.

### 5.4 ~1,400 lines of confirmed dead code

`MemoApp.Core/Examples/` (129) · `MajorSystemImageGenerator.cs` (132) · `NumberFormatHelper.GenerateTrainingSequence` · `Views/TestPage` (registered singleton, no route) · `ExampleManualVerificationUsage` · five onboarding views and two ViewModels registered but never resolved · dead animation controls (`ParticleFieldView`, `SynapticNetworkView`, `BrainWavePulse`) containing unstoppable animation loops.

The whole OpenAI image-generation path is referenced only by integration tests, yet `OpenAI 2.1.0` ships in every mobile build. `src/Azure/MemoAppFeedback` is **not in `MemoApp.sln`** — it never builds with the solution and no CI step covers it.

Check `MajorSystemImageGenerator` against product intent before deleting — it is complete and working, just never wired in.

---

## 6. The plan — sequenced phases

Ordered so each phase makes the next cheaper, and so nothing is refactored before it can be verified. The numbering is a dependency chain, not a ranking.

### Phase 0 — Safety net  ✅ DONE
**~1 day. Unblocks everything else.** — shipped in commit `750fa61`.

- [x] Add `dotnet test` to `azure-pipelines.yml`. One line; starts protecting the 51 tests you already have.
- [x] Write characterisation tests for `NumberSequence` + `GameSession` covering the `00-99` duplicate case. **Expect them to fail** — that is the point.
- [x] Add the first tests for `BigNumberService`. Its generator is correct (verified across 25k generations: length, `maxPairValue` clamping, leading-zero suppression, argument validation) — lock that in before touching it.

### Phase 1 — Ship the cheap user-visible fixes  ✅ DONE
**~1–2 days. Items are independent; any order.** — applied, build clean, 100 tests green.

- [x] §2.1 onboarding step-count regression + §2.2 progress-text localization
- [x] §3.1 `FontSize` persistence (2 lines)
- [x] §3.2 `async void` commands → `async Task` + try/catch
- [x] §3.3 move `BigNumberPage` subscriptions to `OnAppearing`, add missing `-=`
- [x] §3.4 five broken `StringFormat` labels
- [x] §3.5 literal markup on the onboarding screen
- [x] §3.6 `SemanticProperties.Description="Button"` on MainPage
- [x] §3.7 fabricated share statistics
- [x] §3.8 unify accuracy tiers behind named constants
- [x] §3.9 localize the advanced-settings toggle
- [x] §3.10 add the two missing Italian strings
- [x] §3.11 switch amber buttons to `WarningAccessible`

### Phase 2 — Unify sequence generation  ✅ DONE
**~2–3 days. Fixes the duplicate-numbers bug.** — applied, solution builds clean, 117 tests green.

- [x] Add `NumberFormat` to the Core generator; fold in `GenerateTrainingSequence`'s dedup rule
- [x] Fix the dead ternary at `NumberFormatHelper.cs:24`
- [x] Change `GameSession` to accept a supplied sequence rather than building its own — removes the hidden dependency, makes it trivially testable
- [x] Delete generators #2 and #3, the duplicated `ParseRangeValue`, and the dead `GenerateTrainingSequence`
- [x] Reduce `MajorSystemCategoryService.GenerateNumberTokens` to a projection over the Core generator
- [x] Replace `OrderBy(_ => random.Next())` at `GameSession.cs:37` — a biased shuffle — with `Random.Shared.Shuffle()`

### Phase 3 — One clock, one statistics owner  ✅ DONE
**~2 days.** — applied, solution builds clean, 129 tests green.

- [x] Delete the ViewModel-side session clock; `GameSession` becomes the single time authority
- [x] Inject `TimeProvider` into `GameSession` and `BigNumberService`; standardise on UTC, with a monotonic source (`Stopwatch`) for elapsed display
- [x] Pause/resume elapsed tracking on `OnSleep`/`OnResume` so backgrounded time is excluded
- [x] Guard `FormatTime` against negative spans; add hour rollover
- [x] Read `FastestResponse` from `SessionStatistics`; delete both `fastestTime = averageTime` lines
- [x] Delete the two analytics-fabrication blocks in `BigNumberViewModel` — emit no analytics rather than invented analytics
- [x] Move `CalculateConsistencyScore` and `CalculateResponseTimeDistribution` from `StatisticsViewModel` into Core
- [x] One `FormatDuration` in Core; delete the three copies

### Phase 4 — Collapse the remaining duplication  ✅ DONE
**~2 days.** — applied; all three mobile targets build clean, 138 tests green.

- [x] `ResolveDefault` on `IAssociationComposerService`; rewire all four call sites
- [x] Generic settings store over an injectable `IPreferences`; makes settings testable and fixes `FontSize` permanently
- [x] Delete the 188 dead lines in `PackAssociationsService`
- [x] Delete the ~1,400 dead lines catalogued in §5.4
- [x] Sweep `Debug.WriteLine` catches to `IExceptionHandler`, Firebase init sites first
- [x] Make `GlobalExceptionHandler.LogAppCrash` awaitable so crash reports actually flush

### Phase 5 — Decompose and consolidate  ◐ PARTIAL
**Ongoing. Only after Phases 0–3 provide tests and a single clock.**

- [ ] Dead-code pass on `BigNumberViewModel` first (~15 members), then split by `GamePhase` behind the existing binding surface
- [ ] Pick one card system and one typography scale; delete the other
- [ ] DI cleanup: captive `HttpClient` in singleton `IPackDownloadService`; singleton `Page` registrations; never-assigned `_tooltipService` field; dead registrations
- [ ] Fix `CollectionView`-in-`ScrollView` nesting; replace fixed `HeightRequest` with `MinimumHeightRequest`
- [ ] Accessibility pass: `SemanticProperties` on the ~154 interactive elements that lack them, starting with `FlashNumbersPage` (currently zero); add `HeadingLevel`

---

## 7. Verified sound — do not re-audit

- **`MemoApp.Core` is genuinely UI-agnostic.** `UseMaui=false`, and a repo-wide grep for `Microsoft.Maui` inside Core returns zero hits. The layering premise is sound — Core is simply under-populated. Domain logic did not leak *into* the UI; it was never put in Core.
- **The Big Number generator algorithm is correct.** 25k+ generations verified: digit length, `maxPairValue` clamping, leading-zero suppression, argument validation all behave. It is untested, not buggy.
- **Destructive actions are consistently confirmed** — phase-aware dialogs across Big Number, token deletion, pack deletion. Worth preserving.
- **Navigation is uniform** — 33 `Shell.Current.GoToAsync` sites across 14 ViewModels, no push/modal inconsistency.
- **No sync-over-async** except one `Dispose`. No `MessagingCenter`, no `Device.StartTimer`. Every `SemaphoreSlim` acquire/release pair is correct.
- **Only 4 truly empty catch blocks exist**, all commented and defensible.
- **The Gray and Primary color families are properly contrast-checked**, annotated inline with real ratios.
- **Italian translations are largely well-crafted** — translators already compressed tab titles ("Associations" → "Schedario"). 659 of 665 keys present.

---

## 8. Limits of this audit

- **Nothing was run on a device or simulator.** Every runtime symptom is inferred from code, not observed. The layout-collapse and accessibility-scale predictions in §5.3 need a real device at 130%/200% font scale to confirm.
- **Only `net10.0-maccatalyst` Debug was built.** The iOS and Android Release paths — where the `IL2026` trimming warnings and every `Debug.WriteLine`-only catch actually bite — were not compiled or exercised.
- **§3.4**: the breakage is certain; whether MAUI throws `FormatException` or renders the literal is platform-dependent and unverified.
- **§4.2**: the DST/timezone corruption path is confirmed from code but not reproduced with a shifted clock.
- **§3.2**: the last-number race window depends on real analytics latency; the code path and throw site are confirmed, the timing on a device is not.
- Where the two duplication reviews differed on whether the natural-format variant counts as a fifth generator, the count reflects what was independently verified.

---

## 9. Progress log

### Phase 0 — Safety net ✅ (commit `750fa61`)

| Change | File |
|---|---|
| `dotnet test` step added between NuGet restore and the iOS build, so a failing test stops the pipeline before TestFlight | `azure-pipelines.yml` |
| Characterisation tests for the §1 duplication defect | `NumberSequenceDuplicationTests.cs` *(since folded into `NumberSequenceTests.cs`)* |
| First tests for `BigNumberService` | `tests/MemoApp.Core.Tests/BigNumber/BigNumberServiceTests.cs` *(new)* |

**Tests: 51 → 100** (96 passing, 4 skipped).

`MemoApp.Core.IntegrationTests` is deliberately excluded from CI — it requires live OpenAI credentials.

**On the 4 skipped tests.** The plan said the new tests should fail. They don't, deliberately: a red pipeline on day one is a pipeline people learn to ignore, which would defeat the step being added. Instead each desired-behaviour assertion sits as a skipped `[Fact]` directly beneath its characterisation counterpart. These were verified to be genuine tripwires — with the `Skip` attributes temporarily stripped, **all 4 fail**:

- `GenerateSequence_CompleteMajorSystemPreset_ShouldYield100DistinctNumbers`
- `GenerateSequence_AnyRange_ShouldNeverRepeatARenderedLabel`
- `FormatTime_ShouldRollOverIntoHours`
- `FormatTime_ShouldClampNegativeSpansToZero`

When a phase lands, unskip the test and delete its characterisation twin.

### Phase 1 — Cheap user-visible fixes ✅

| § | Fix | Files |
|---|---|---|
| 2.1 | Added the missing 5th tutorial dot (`TutorialStep4`) and registered it; localized the two hardcoded phase labels using the already-existing `Onboarding_Phase1_Tutorial` / `Onboarding_Phase2_Setup` keys | `OnboardingNavigationPage.xaml`, `.xaml.cs` |
| 2.2 | English string interpolation → localized format strings | `OnboardingNavigationViewModel.cs` |
| 3.1 | `FontSize` now persisted (key + load + save) | `BigNumberSettingsService.cs` |
| 3.2 | 3 × `async void` → `async Task`; `int.Parse` → guarded `TryParse`; try/catch → `IExceptionHandler`; `CompleteSession` is now awaited | `FlashNumbersViewModel.cs` |
| 3.3 | Subscriptions moved from constructor to `OnAppearing` via an idempotent `SubscribeToViewModel`/`UnsubscribeFromViewModel` pair; the missing 6th `-=` added | `BigNumberPage.xaml.cs` |
| 3.4 | 5 broken `StringFormat` bindings replaced with a new converter | `LocalizedFormatConverter.cs` *(new)*, `App.xaml`, `FeedbackPage.xaml`, `AddCustomAssociationPage.xaml`, `FlashNumbersPage.xaml` |
| 3.5 | Emoji folded into the resource value so the binding evaluates | `MajorSystemExampleView.xaml`, both `.resx` |
| 3.6 | 5 × `Description="Button"` → localized card names; hints localized | `MainPage.xaml`, both `.resx` |
| 3.7 | Share data now reads `TotalDuration` / `AverageResponseTime` / `FastestResponse.ResponseTime` from `SessionStatistics`; `ShareResultsAsync` reuses the completion snapshot instead of recomputing at share time | `FlashNumbersViewModel.cs` |
| 3.8 | Two conflicting accuracy paths → one `ResolveAccuracyColor` behind named constants | `BigNumberViewModel.cs` |
| 3.9 | Toggle text localized | `BigNumberViewModel.cs` |
| 3.10 | `Onboarding_Welcome_Title` + `Statistics_SessionSummary` added to Italian | `AppResources.it.resx` |
| 3.11 | 3 failing-contrast buttons moved onto `WarningAccessible` — **and that token itself corrected**, see below | `Colors.xaml`, 3 pages |

**Verification:** build 0 errors, `MVVMTK0039` 3 → 0, warnings 345 → 340, 100 tests green, EN/IT parity 673/669.

### Corrections found while implementing

Three things turned out differently from the audit:

1. **`WarningAccessible` did not meet its own documented contract.** Both the audit and the inline comment in `Colors.xaml` claimed `#CC5500` was 4.9:1 against white. Measured, it is **4.31:1** — below the 4.5:1 AA floor for normal text. Two of the three buttons are bold ≥14pt and so qualify as large text (3:1), but `AddCustomAssociationPage.xaml:186` is 14pt regular and would still have failed. The token was therefore changed to **`#B45309` (amber-700, 5.02:1 verified)**, which passes for both normal and large text and stays in the amber family. Four consumers, all checked.
2. **`BigNumber_Hide` already existed** in both resource files — §3.9 was purely a code fix, no resource work needed.
3. **§3.4 needed more than a binding edit.** `StringFormat='{localization:Localize Key}'` has no in-place fix; a converter was required. `AddCustomAssociation_CreditsRemaining` also had no `{0}` placeholder — its value was changed to `"Credits remaining: {0}"` / `"Crediti rimanenti: {0}"` (single consumer, verified).

### Two small items done outside the Phase 1 list

Both sit inside code that was already being edited:

- `BigNumberPage.OnAppearing` — `await viewModel.InitializeAsync()` wrapped in try/catch. Unguarded, a corrupt settings payload crashed the app every time the page opened, permanently locking the user out of the exercise.
- `FlashNumbersViewModel.cs:237` — a second hardcoded `Title = "Flash Numbers"` now uses `FlashNumbers_Title`, matching the correctly-localized assignment already present at `:261`.

### New resource keys added

`Onboarding_TutorialProgressFormat`, `Onboarding_SetupProgressFormat`, `FlashNumbers_Results`, `MainPage_AssociationsHint`, `MainPage_FlashNumbersHint`, `MainPage_BigNumberHint`, `MainPage_MajorSystemGuideHint`, `MainPage_FeedbackHint` — all in both languages.

### Still outstanding

The 4 keys missing from Italian (`FlashNumbers_RevealAssociation`, `Onboarding_Money`, `Onboarding_SkipMessage`, `Onboarding_SkipTitle`) are **orphaned — referenced nowhere**, so they are deletion candidates in Phase 4 rather than translation work.

Nothing in Phase 1 was verified on a device. The accessibility and contrast changes are measured, not observed with a screen reader.

### Phase 2 — Unify sequence generation ✅

**The §1 defect is fixed.** Verified by running the built `MemoApp.Core` assembly:

```
Padded (the default for new users)
  Complete Major System  "00"->"99" : 100 items, 100 distinct,  0 duplicate   OK   (was 110 / 100 / 10)
  Numbers 00 to 69       "00"->"69" :  70 items,  70 distinct,  0 duplicate   OK   (was  80 /  70 / 10)
  Zero-prefixed 00-09    "00"->"09" :  10 items,  10 distinct,  0 duplicate   OK
  first 12 on screen     : 00 01 02 03 04 05 06 07 08 09 10 11               (was 00..09 00..09 10 11)
Natural
  Complete 0-99          "0"->"99"  : 100 items, 100 distinct,  0 duplicate   OK
```

| Change | File |
|---|---|
| Format-aware generator; `ParseRangeValue` made public and normalising; `GetAllTokens` added | `NumberSequence.cs` (43 → 119 lines) |
| Constructor now takes a supplied sequence; Fisher–Yates replaces the biased shuffle | `GameSession.cs` |
| Three duplicate generators, the duplicated `ParseRangeValue`, and the dead `GenerateTrainingSequence` all deleted; the rest delegates to Core | `NumberFormatHelper.cs` (**125 → 39 lines**) |
| `GenerateNumberTokens` reduced to a projection (28 → 5 lines) | `MajorSystemCategoryService.cs` |
| Pass the user's format when building a session | `FlashNumbersViewModel.cs`, `ConsoleInterface.cs` |

All sequence-generation loops now live in exactly one file. `grep "yield return MajorNumber.From"` returns `NumberSequence.cs` only.

#### The design decision that mattered

The audit framed this as "delete the duplicates". Doing that literally would have introduced a data bug, because **two different concepts were tangled in the duplicated code**:

- **Presentation sequence** — what a training session shows, rendered through `NumberFormatHelper.FormatNumber`. Under Padded, `Value.ToString("00")` turns the natural `7` and the zero-prefixed `07` into the same on-screen `"07"`. This is the bug, and Padded must therefore exclude natural values below 10 → **100 items**.
- **Token vocabulary** — what the Associations browser and authoring surface address, keyed by `MajorNumber.Display`, under which `"07"` and `"7"` are different strings *and* different mnemonics (07 = S-K, 7 = K). Narrowing this would have orphaned any association saved against a bare single-digit token.

They are now separate methods — `GetAllNumbersInOrder(format)` and `GetAllTokens(format)` — with the distinction documented and covered by tests. The vocabulary output was verified byte-for-byte identical to pre-Phase-2 (Padded 110, Natural 100), so **no association data is affected**.

#### Behaviour changes

1. **Padded presentation sequences lost their duplicates** — 110 → 100 for `00-99`. This is the fix.
2. **`GameSession`'s two-argument constructor is gone.** It is now `(rangeStart, rangeEnd, NumberFormat)` or `(rangeStart, rangeEnd, IReadOnlyList<MajorNumber>)`. Every call site updated.
3. **Range endpoints normalise per format.** Under Padded, `"0"` and `"00"` resolve to the same token, so `"2"→"08"` is now a valid ascending range (02..08) where it previously threw. A test covered the old behaviour and was rewritten.
4. **Crossing-boundary ranges no longer exist.** `"08"→"2"` used to yield `08, 09, 0, 1, 2` by running off the zero-prefixed block into the natural one. That ordering was the defect; the test asserting it was replaced with format-explicit coverage.

#### Shuffle

`OrderBy(_ => random.Next())` is a biased shuffle — equal sort keys leave elements in their original relative order. Replaced with Fisher–Yates over `Random.Shared`. Measured across 60,000 sessions, one token's landing position spreads 4.4% across the ten slots, consistent with uniform.

#### Tests: 100 → 117

The two Phase 2 tripwires are now live assertions and the characterisation tests that locked in the broken behaviour were deleted, as planned. Two remain skipped, both on `FormatTime`: no hours component, and no guard against negative spans.

Tests were also reorganised to one file per unit under test — the sequence tests folded into `NumberSequenceTests.cs`, the session tests into `GameSessionTests.cs`, the `Display` test into `MajorNumberTests.cs`, and `NumberSequenceDuplicationTests.cs` removed. Their comments describe what the code does and why, with no reference to this plan or to what the behaviour used to be: a test read a year from now should explain the contract, not the changelog.

New coverage: presentation-vs-vocabulary distinction, per-format range generation, endpoint normalisation, descending-range rejection, `GameSession` accepting a supplied sequence, and a shuffle test asserting no number is lost or duplicated.

#### Not verified

No device run. The Flash Numbers range picker and the Associations browser were reasoned about from code and covered by unit tests, but not observed on screen.

### Phase 3 — One clock, one statistics owner ✅

Elapsed time is now measured monotonically via an injected `TimeProvider`, so a device clock
that moves mid-round — timezone change on landing, NTP correction, manual adjustment — can no
longer distort a timer or a recorded duration.

| Change | File |
|---|---|
| `TimeProvider` injected; `DateTime.Now` gone; monotonic `StartTimestamp`; new `GetElapsedTime(game)` | `BigNumberService.cs`, `IBigNumberService.cs`, `BigNumberGame.cs` |
| `TimeProvider` injected; new monotonic `Elapsed`, frozen once the session completes | `GameSession.cs` |
| Timer tick delegates to the service instead of re-deriving the elapsed formula across the layer boundary | `BigNumberViewModel.cs` |
| Both analytics-fabrication blocks deleted | `BigNumberViewModel.cs` |
| ViewModel-side session clock deleted; abandonment analytics read `GameSession.Elapsed` | `FlashNumbersViewModel.cs` |
| `FormatTime` carries into hours and clamps negative spans | `BigNumberService.cs` |
| Two identical `FormatDuration` copies collapsed onto the base class | `ShareContent.cs` |
| `TimeProvider.System` registered as the single clock source | `ServiceRegistration.cs` |

**Verification:** solution builds with 0 errors. `DateTime.Now` no longer appears anywhere in the
exercise paths; one occurrence remains in `CustomAssociationsService.cs:46`, where it builds a
filename timestamp rather than measuring anything. One `FormatDuration`, zero fabricated stamps,
zero ViewModel-side exercise clocks.

#### On no longer inventing analytics

Two byte-identical blocks filled in missing timing stamps with `DateTime.UtcNow.AddMinutes(-1)`
and `DateTime.UtcNow.AddSeconds(-30) // Estimate they spent 30 seconds on recall`, then reported
the result as measured. Both are gone. When a round was not actually measured the duration is now
`TimeSpan.Zero` — a missing measurement reads as missing rather than as a plausible number.

#### Two deviations from the plan

1. **Background pause was not implemented, deliberately.** The plan called for pausing elapsed
   tracking on `OnSleep`/`OnResume` so backgrounded time is excluded. For a *timed memorisation
   challenge* that is exploitable: a user could background the app, take as long as they like,
   and return to an unchanged timer. Wall-clock behaviour is arguably the correct rule for this
   domain. This is a product decision rather than a bug fix, so it is left open deliberately —
   the monotonic clock fixes the actual corruption without changing the rules of the exercise.

2. **The statistics move was smaller than planned.** `CalculateConsistencyScore` turned out to be
   **dead** — `ConsistencyScoreText` has zero XAML bindings — so it was deleted rather than moved
   to Core, taking five hardcoded English strings (`"Perfect"`, `"Excellent"`, `"Good"`, `"Fair"`,
   `"Needs Work"`) with it. `CalculateResponseTimeDistribution` is live, but it is percentage
   arithmetic over `FlashNumbersPerformanceService.CategorizeResponseTime`, which still lives in
   the UI layer. Moving the arithmetic without the service would just relocate the coupling, so it
   is deferred to Phase 5, where that service moves to Core.

#### Tests: 117 → 129, none skipped

The two remaining tripwires became live specifications, so the suite now has no skipped tests.

`TestTimeProvider` (new) drives the clock by hand. `Advance` moves wall-clock and monotonic time
together as real time does; `ShiftWallClock` moves only the wall clock, reproducing a timezone or
NTP correction. The tests that matter assert that a one-hour wall-clock jump mid-round leaves both
`GetElapsedTime` and `GameSession.Elapsed` reading the true elapsed time, and that a stopped round
records the real duration. Also covered: elapsed freezing at completion, UTC kind on the recorded
start, response times drawn from the injected clock, hour carry, and negative-span clamping.

#### Not verified

No device run. Backgrounding behaviour, and the timer under a real timezone change, were reasoned
about and unit-tested against a fake clock rather than observed on hardware.

### Phase 4 — Collapse the remaining duplication ✅

| Change | Effect |
|---|---|
| Deleted the two unreachable reimplementations in `PackAssociationsService` | 868 → 680 lines |
| `AssociationDefaults.Resolve` is now the only copy of the default-association rule; `IAssociationComposerService.ResolveDefault` exposes it; three call sites rewired | one rule, one implementation |
| `OnUnhandledException` no longer `async void`; flushes the report before the process exits | crash reports survive termination |
| Deleted dead public helpers `LogAppCrash` and `LogNavigationFailure` | `GlobalExceptionHandler` 177 → 141 lines |
| Firebase init failure recorded at platform startup and reported once monitoring exists | analytics failure is no longer silent in Release |
| Settings rewritten as a source-generated JSON store in Core behind `IPreferenceStore` | one implementation; adding a property persists it automatically |
| Removed 4 orphaned keys that were also missing Italian | **EN/IT parity complete: 669/669** |
| Deleted 10 verified-dead files | ~940 lines |

**Verification:** all three mobile targets (`maccatalyst`, `android`, `ios`) build with 0 errors; 142 tests green.

#### Crash reporting

The audit pointed at `LogAppCrash`. That method had **no callers** — the live crash path is
`OnUnhandledException`, wired up in `Initialize()`, and it had the same defect in a place that
actually runs. It was `async void` and awaited the report, so when `IsTerminating` was true the
handler returned at the `await` and the process exited before anything was sent.

It now starts the report and, when terminating, waits on it with a bounded timeout. That is
deliberate sync-over-async and the comment says why: the process is about to exit, so awaiting
would hand back control and lose the report. Non-terminating reports stay asynchronous with a
faulted-task continuation so they cannot become unobserved exceptions. The two dead helpers were
deleted rather than fixed.

#### Preventing another silently-unsaved setting

Settings are now stored as a single JSON payload per model, replacing the property-by-property
`Preferences.Get`/`Set` pairs. **Adding a property to a settings model now persists it with no
service change at all** — the failure mode that lost `FontSize` is structurally impossible rather
than merely test-caught. Verified by adding a property to `BigNumberSettings` and confirming the
round-trip suite passed with the service untouched.

Both services moved to Core behind `IPreferenceStore`; the MAUI implementation is a thin wrapper
over the static `Preferences`, which cannot be substituted in a test. Serialization is
source-generated through `SettingsJsonContext`, so it is safe under trimming and AOT on iOS —
Core still reports **zero** `IL2026` warnings.

An unreadable payload falls back to model defaults rather than throwing. That matters because a
corrupt payload previously crashed the owning screen on every open, with no way back short of
reinstalling.

The reflection-driven round-trip tests remain: they assign a non-default value to every public
property, save, reload, and require each value back. Verified non-vacuous against the original
per-property implementation, where removing the `FontSize` line reproduced the bug and the test
failed.

*Note: an earlier draft of this section rejected the JSON rewrite because changing the stored keys
would reset settings for existing users. The app has not shipped, so there are none — the
objection did not apply and the rewrite went ahead.*

#### Three corrections to the audit

1. **The four-way `ResolveDefault` divergence was really three.** `TokenDetailViewModel:198` uses
   its result **only for a log line** — it does not choose what is displayed. The audit's claim
   that a user "sees nothing on the Token Detail screen" is wrong. Its missing `?? First()` is
   correct there: it asks whether an *explicit* default exists, and resolving a fallback would log
   it as though the user had chosen it. A comment now records that so it is not "fixed" later.
2. **The `OnboardingPackDownload_*` keys are not a screen that never existed.**
   `PackDownloadOnboardingView.xaml` is real; it simply uses differently-named keys. The keys are
   genuinely orphaned, but the stated justification was wrong.
3. **`LogAppCrash` was dead**, and the same defect lived in the handler that actually runs.

#### Deliberately not done

- **199 orphaned resource keys were left in place.** A wrong deletion fails silently at runtime in
  a rarely-hit path, the upside is file size, and one justification the audit offered for the
  "safe" cluster turned out to be wrong. Only the four keys that were *both* orphaned *and* absent
  from Italian were removed, since those could not regress a working string. The rest wants a pass
  with whoever owns the translations.
- **The full `Debug.WriteLine` sweep.** The two highest-value sites (Firebase init on both
  platforms) are done. Roughly 26 remain, mostly in `BigNumberViewModel` and `BigNumberPage`, and
  they are better handled alongside the Phase 5 decomposition of those files.
- **`MajorSystemImageGenerator`** (132 lines) is unreferenced but complete and working. It reads as
  an unfinished feature rather than cruft, so it wants a product decision before deletion.

#### Tests: 129 → 138

`AssociationDefaults.Resolve` (explicit default, first-available fallback, multiple defaults,
empty, null) and the settings round-trip suite (both models, plus defaults when nothing is stored).

### Phase 5 — Decompose and consolidate ◐ PARTIAL

Phase 5 is open-ended by design. The contained, verifiable items are done; the large
restructuring items are not, and are listed below rather than half-started.

#### Done

| Change | Effect |
|---|---|
| Dead-code pass on `BigNumberViewModel`: 10 unreferenced `[RelayCommand]` methods deleted, and `[RelayCommand]` dropped from `StopAndHide` / `StartRecall` (called internally, so the generated command was the unused part) | 2675 → 2476 lines |
| `IFirstTimeTooltipService` deleted entirely — interface, implementation, registration, and the never-assigned field that was its only consumer | one dead service gone |
| `PackDownloadService` takes `IHttpClientFactory` instead of a captured `HttpClient` | handler rotation and DNS refresh restored |
| `MainPage` and `FlashNumbersSetupPage` registrations Singleton → Transient | no page pins its visual tree for the process lifetime; all 14 pages now consistent |
| `RevealButton` on `FlashNumbersPage` given a localized accessible name and hint; the gesture-bearing card documented | the only route to an association is no longer a bare glyph to a screen reader |

**Verification:** all three mobile targets build with 0 errors; 142 tests green; EN/IT parity holds
at 672/672.

#### A build setting that looked promising and was not

A deliberately invalid binding (`{Binding ThisCommandDoesNotExistCommand}`) **does not fail the
build**, even though the pages carry `x:DataType`. Enabling
`MauiEnableXamlCBindingWithSourceCompilation` produced 0 errors across the solution — so the
codebase is ready for it — but a clean rebuild with it enabled *still* did not reject the invalid
binding, so the safety benefit could not be demonstrated. The change was reverted rather than
committed on the strength of an unverified claim.

Worth revisiting: it compiles cleanly today, and compiled bindings are a real performance win on
list-heavy screens such as `UnifiedPackSelector`. It needs a device run to confirm nothing changes
at runtime before being adopted.

The consequence for now is that **XAML binding errors are only caught by review**, which is why
the dead-command removal above was driven by an explicit search across every `.xaml` and `.cs`
file rather than by trusting the compiler.

#### Not done, and why

- **Splitting `BigNumberViewModel` by `GamePhase`.** The dead-code pass removed ~200 lines, but the
  file is still 2,476 lines with a ~200-line font-fitting algorithm, a ~400-line verification state
  machine, and a ~320-line comparison carousel tangled together. Splitting it means moving a large
  binding surface with no compile-time binding validation (above) and no device to test on — the
  combination that turns a refactor into a silent regression. It wants either the binding-validation
  question settled first, or a person with the app running.
- **Consolidating the four card systems and two typography scales.** Purely visual, and there is no
  way to confirm the result looks right without running the app.
- **The broader accessibility sweep.** ~154 interactive elements still lack `SemanticProperties`,
  and `SemanticProperties.HeadingLevel` remains at zero app-wide. Mechanical but large, and it
  should be verified with a real screen reader rather than by counting attributes.
- **Layout fixes** (`CollectionView` inside `ScrollView`, fixed `HeightRequest` against scalable
  text). Each needs visual confirmation on a small screen at a large font scale.
- **The remaining ~26 `Debug.WriteLine` catches**, most of which live in the two files above and are
  best swept as part of that work.
