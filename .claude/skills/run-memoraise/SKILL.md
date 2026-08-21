---
name: run-memoraise
description: Build, run, and drive the MemoRaise .NET MAUI app on an iOS simulator. Use when asked to start MemoRaise, launch the app, take a screenshot of its UI, tap through a screen, verify a change in the running app, or run its tests.
---

MemoRaise is a .NET MAUI app (iOS, Android, Mac Catalyst). An agent drives it on the
**iOS simulator**: `.claude/skills/run-memoraise/smoke.sh` builds, installs, launches and
taps through it, and `simctl-lib.sh` next to it exposes `sim_tap` / `sim_swipe` /
`sim_shot` for anything more specific.

All paths below are relative to the repository root. This is a **macOS-only** workflow —
it needs Xcode's simulators.

## Prerequisites

Xcode with iOS simulators, the .NET 10 SDK with the MAUI workload, and `cliclick` for
taps (`simctl` has no tap command of its own):

```bash
brew install cliclick
```

**Grant your terminal Accessibility permission** — System Settings → Privacy & Security →
Accessibility. `cliclick` synthesizes clicks and silently does nothing without it.
Screen Recording is *not* needed: screenshots come from `simctl`, not the screen.

## Build

```bash
dotnet build src/MemoApp.UI.MauiApp/MemoApp.UI.MauiApp.csproj \
  -f net10.0-ios -p:RuntimeIdentifier=iossimulator-arm64
```

The `RuntimeIdentifier` is required — without it the build targets a device and produces
no simulator-installable bundle. Output:
`src/MemoApp.UI.MauiApp/bin/Debug/net10.0-ios/iossimulator-arm64/MemoApp.UI.MauiApp.app`

Fastest compile-only check (no simulator involved): `-f net10.0-maccatalyst`.

## Run (agent path)

Boot one simulator first, then:

```bash
xcrun simctl boot "iPhone 16"          # any device; pick one that is installed
./.claude/skills/run-memoraise/smoke.sh
```

```
device : 962971EB-…  (393 852 logical pt)
bundle : built 2026-08-21 17:35
launch : alive=1
shot   : /tmp/memoraise-shots/01-launch-view.png
tap    : Big Number tab -> alive=1
shot   : /tmp/memoraise-shots/02-bignumber-view.png
```

**Look at the `-view.png` files.** A blank frame means the launch failed.

| variable | effect |
|---|---|
| `SKIP_BUILD=1` | drive an already-installed build (seconds instead of minutes) |
| `SIM_UDID=<udid>` | choose the device; **required** when more than one is booted |

For anything beyond the smoke path, source the library:

```bash
source .claude/skills/run-memoraise/simctl-lib.sh
sim_tap 0.70 0.928          # fraction of the device screen: x, y
sim_swipe 0.5 0.75 0.25     # x, from-y, to-y
sim_shot /tmp/s.png         # prints the path of a scaled, readable copy
sim_alive                   # 1 while the app process is running
```

Coordinates are **fractions of the device screen**, so the same call works on any
simulator. The library derives logical size from the device's `profile.plist`
(`mainScreenWidth / mainScreenScale`) — iPhone 16 is 393×852, iPhone 17 Pro is 402×874,
iPad Pro is 834×1210 at @2x.

### Inspecting on-device state

Often faster than driving the UI:

```bash
C=$(xcrun simctl get_app_container <udid> com.stefanoottaviani.memoapp data)
plutil -p "$C/Library/Preferences/com.stefanoottaviani.memoapp.plist"
xcrun simctl pbpaste <udid>        # read what the app copied to the clipboard
```

Settings are stored as JSON under `BigNumber_Settings` / `FlashNumbers_Settings`.
To skip the ~9-screen onboarding gate:

```bash
xcrun simctl spawn <udid> defaults write com.stefanoottaviani.memoapp \
  OnboardingCompleted -string "true"
```

## Test

```bash
dotnet test tests/MemoApp.Core.Tests/
```

145 tests, none skipped. `MemoApp.Core.IntegrationTests` is separate and needs live
OpenAI credentials — CI excludes it deliberately.

## Gotchas

- **Never `simctl install booted`.** With two devices booted it silently picks one. This
  cost an afternoon debugging a "bug" that was a build from ten months earlier, still
  installed on the other simulator. `sim_udid` refuses and lists them instead.
- **Check the installed bundle's date before trusting any result** — `smoke.sh` prints it.
  A stale build looks exactly like a regression.
- **Binding errors do not fail the build.** In Debug, .NET 10 MAUI inflates XAML at
  runtime, so `{Binding NoSuchProperty}` compiles clean and fails silently on screen.
  `MauiEnableXamlCBindingWithSourceCompilation` and `MauiStrictXamlCompilation` do not
  change this; `MauiXamlInflator=SourceGen` emits real diagnostics but falls back to
  `XamlCompilationOptions.Skip` for every page that has bindings. **After renaming a bound
  member, look at the screen.**
- **`osascript … click at {x,y}` does not work** on the Simulator — it does not move the
  cursor and the click lands nowhere. Use `cliclick`, and activate the Simulator window
  first (`sim_tap` does both).
- **Screenshot the whole screen scaled, not a guessed crop.** `sim_shot` writes a
  `-view.png` for this. Cropping blind twice produced confident wrong readings here — a
  loading state read as "no data", and an action sheet read as "a hung modal".
- **Comparing screenshots by hash is fooled by the clock.** Crop the status bar off first:
  `sips -c 2300 <width> a.png --out b.png`.
- **Editing source with Python strips CRLF.** Several files here are CRLF; a naive
  `open(p).read()` / `write()` turns a one-line change into a whole-file diff. Read and
  write binary, or preserve `newline=''`.

## Troubleshooting

- **`error NETSDK1005: … does not contain a target for 'net9.0-ios'`**: the project targets
  `net10.0-*`. Use `-f net10.0-ios`.
- **`simctl install` succeeds but the app looks old**: it went to a different booted
  device. Set `SIM_UDID` and reinstall.
- **Taps do nothing, no error**: either the terminal lacks Accessibility permission, or the
  Simulator window is not frontmost.
- **`Invalid index. (-1719)` from System Events**: the Simulator has no window. Run
  `open -a Simulator --args -CurrentDeviceUDID <udid>`.
- **Screen dims and stops responding**: an action sheet or system dialog is open — the
  Apple Account prompt appears often. Screenshot the whole screen to see it, then dismiss.
