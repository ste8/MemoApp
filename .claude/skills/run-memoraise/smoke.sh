#!/bin/bash
# Build, install, launch and drive MemoRaise on an iOS simulator.
#
#   ./.claude/skills/run-memoraise/smoke.sh            # full: build + install + drive
#   SKIP_BUILD=1 ./.claude/skills/run-memoraise/smoke.sh   # drive an already-installed build
#   SIM_UDID=<udid> ./...                              # choose the device explicitly
#
# Screenshots land in /tmp/memoraise-shots/. Each is written twice: the raw
# device capture, and a *-view.png scaled for reading at a glance.

set -euo pipefail
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$HERE/../../.." && pwd)"
source "$HERE/simctl-lib.sh"

APP_PROJ="$ROOT/src/MemoApp.UI.MauiApp/MemoApp.UI.MauiApp.csproj"
APP_BUNDLE="$ROOT/src/MemoApp.UI.MauiApp/bin/Debug/net10.0-ios/iossimulator-arm64/MemoApp.UI.MauiApp.app"
SHOTS=/tmp/memoraise-shots
mkdir -p "$SHOTS"

UDID=$(sim_udid)
echo "device : $UDID  ($(sim_logical_size "$UDID") logical pt)"

if [ -z "${SKIP_BUILD:-}" ]; then
  echo "build  : net10.0-ios / iossimulator-arm64 (slow the first time)"
  dotnet build "$APP_PROJ" -f net10.0-ios -p:RuntimeIdentifier=iossimulator-arm64 -v q --nologo \
    | grep -E "error|Errori:|Error" || true
fi

[ -d "$APP_BUNDLE" ] || { echo "no app bundle at $APP_BUNDLE — build first" >&2; exit 1; }
echo "bundle : built $(date -r "$APP_BUNDLE" '+%Y-%m-%d %H:%M')"

xcrun simctl terminate "$UDID" "$BUNDLE_ID" >/dev/null 2>&1 || true
xcrun simctl install "$UDID" "$APP_BUNDLE"
open -a Simulator --args -CurrentDeviceUDID "$UDID" >/dev/null 2>&1 || true
sleep 5
xcrun simctl launch "$UDID" "$BUNDLE_ID" >/dev/null
sleep 9

echo "launch : alive=$(sim_alive)"
echo "shot   : $(sim_shot "$SHOTS/01-launch.png")"

# Drive something: the tab bar sits at y≈0.93 with five evenly spaced tabs.
sim_tap 0.70 0.928 >/dev/null 2>&1 || true
sleep 4
echo "tap    : Big Number tab -> alive=$(sim_alive)"
echo "shot   : $(sim_shot "$SHOTS/02-bignumber.png")"
echo
echo "Open the *-view.png files. A blank frame means the launch failed."
