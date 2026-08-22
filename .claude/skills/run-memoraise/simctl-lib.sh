#!/bin/bash
# Shared helpers for driving the app on an iOS simulator.
#
# Screen geometry is DERIVED, never hardcoded: every device type ships a
# profile.plist with mainScreenWidth/Height/Scale, so logical points are
# width/scale. An iPhone 16 is 393x852 but an iPhone 17 Pro is 402x874 and an
# iPad Pro is 834x1210 at @2x - a hardcoded size makes taps drift, worst at the
# screen edges where tab bars and nav buttons live.

set -euo pipefail

BUNDLE_ID="${BUNDLE_ID:-com.stefanoottaviani.memoapp}"

# Resolve exactly one target device. Never uses `booted`: with two devices up,
# `simctl install booted` silently picks one, which once cost an afternoon
# debugging a months-old build that was installed on the other simulator.
sim_udid() {
  if [ -n "${SIM_UDID:-}" ]; then echo "$SIM_UDID"; return; fi
  local booted
  booted=$(xcrun simctl list devices | grep Booted | grep -oE '[0-9A-F-]{36}' || true)
  local count
  count=$(printf '%s\n' "$booted" | grep -c . || true)
  if [ "$count" -eq 0 ]; then
    echo "No booted simulator. Boot one, or set SIM_UDID=<udid>." >&2; return 1
  fi
  if [ "$count" -gt 1 ]; then
    echo "More than one simulator is booted; set SIM_UDID to choose:" >&2
    xcrun simctl list devices | grep Booted >&2
    return 1
  fi
  printf '%s' "$booted"
}

# Logical point size of the device screen: "<width> <height>"
sim_logical_size() {
  local udid="$1"
  python3 - "$udid" <<'PY'
import json, subprocess, sys, os, re
udid = sys.argv[1]
devs = json.loads(subprocess.run(['xcrun','simctl','list','devices','-j'],
                                 capture_output=True, text=True).stdout)
name = None
for runtime, items in devs['devices'].items():
    for d in items:
        if d['udid'].upper() == udid.upper():
            name = d['name']
if not name:
    sys.exit(f"udid {udid} not found")
types = json.loads(subprocess.run(['xcrun','simctl','list','devicetypes','-j'],
                                  capture_output=True, text=True).stdout)
bundle = next((t['bundlePath'] for t in types['devicetypes'] if t['name'] == name), None)
if not bundle:
    sys.exit(f"no device type named {name!r}")
plist = os.path.join(bundle, 'Contents/Resources/profile.plist')
def val(key):
    r = subprocess.run(['/usr/libexec/PlistBuddy','-c',f'Print :{key}',plist],
                       capture_output=True, text=True)
    return float(r.stdout.strip())
w, h, s = val('mainScreenWidth'), val('mainScreenHeight'), val('mainScreenScale')
print(f"{w/s:.0f} {h/s:.0f}")
PY
}

# Window geometry as "<x> <y> <w> <h>", after making sure the Simulator is
# rendering at 1:1.
#
# The Simulator scales large devices down to fit the display - an iPad Pro 11
# (834x1210) renders at about 79% on a laptop. The point->screen mapping below
# assumes 1:1, so a scaled window makes every tap land somewhere else, silently
# and with no error. "Point Accurate" (Window menu) forces 1:1; if the device is
# too large to fit even then, this fails loudly rather than mis-tapping.
sim_window() {
  local udid="$1" dw dh
  read -r dw dh <<< "$(sim_logical_size "$udid")"
  osascript -e 'tell application "Simulator" to activate' >/dev/null 2>&1
  sleep 0.4
  local win ww
  win=$(osascript -e 'tell application "System Events" to tell process "Simulator" to get {position, size} of window 1' 2>/dev/null) || {
    echo "Simulator has no window. Run: open -a Simulator --args -CurrentDeviceUDID $udid" >&2; return 1; }
  ww=$(echo "$win"|cut -d, -f3|tr -d ' ')
  if [ "$(python3 -c "print(1 if $ww < $dw else 0)")" = "1" ]; then
    # scaled - try to force 1:1 and re-read
    osascript -e 'tell application "System Events" to tell process "Simulator" to click menu item "Point Accurate" of menu 1 of menu bar item "Window" of menu bar 1' >/dev/null 2>&1
    sleep 1.5
    win=$(osascript -e 'tell application "System Events" to tell process "Simulator" to get {position, size} of window 1' 2>/dev/null)
    ww=$(echo "$win"|cut -d, -f3|tr -d ' ')
  fi
  if [ "$(python3 -c "print(1 if $ww < $dw else 0)")" = "1" ]; then
    echo "Simulator window (${ww}pt wide) is narrower than the device (${dw}pt): it is still" >&2
    echo "being scaled, so taps would land in the wrong place. Use a smaller device, or" >&2
    echo "enlarge the display. Screenshots (sim_shot) are unaffected." >&2
    return 1
  fi
  # A window at a negative position is off-screen: cliclick would click into empty
  # space and every tap would silently do nothing. Pull it back into view.
  local wx wy
  wx=$(echo "$win"|cut -d, -f1|tr -d ' '); wy=$(echo "$win"|cut -d, -f2|tr -d ' ')
  if [ "$(python3 -c "print(1 if $wx < 0 or $wy < 0 else 0)")" = "1" ]; then
    osascript -e 'tell application "System Events" to tell process "Simulator" to set position of window 1 to {100, 60}' >/dev/null 2>&1
    sleep 0.6
    win=$(osascript -e 'tell application "System Events" to tell process "Simulator" to get {position, size} of window 1' 2>/dev/null)
  fi

  echo "$win" | tr -d ' ' | tr ',' ' '
}

# Tap at a fraction (0-1) of the device screen.
sim_tap() {
  local fx="$1" fy="$2" udid; udid=$(sim_udid)
  local wx wy ww wh dw dh
  read -r wx wy ww wh <<< "$(sim_window "$udid")" || return 1
  read -r dw dh <<< "$(sim_logical_size "$udid")"
  local x y
  x=$(python3 -c "print(int($wx+($ww-$dw)/2+$fx*$dw))")
  y=$(python3 -c "print(int($wy+$wh-$dh-($ww-$dw)/2+$fy*$dh))")
  cliclick "m:$x,$y" w:120 "c:$x,$y"
}

# Swipe between two vertical fractions at a given horizontal fraction.
sim_swipe() {
  local fx="$1" f1="$2" f2="$3" udid; udid=$(sim_udid)
  local wx wy ww wh dw dh
  read -r wx wy ww wh <<< "$(sim_window "$udid")" || return 1
  read -r dw dh <<< "$(sim_logical_size "$udid")"
  local x y1 y2
  x=$(python3 -c "print(int($wx+($ww-$dw)/2+$fx*$dw))")
  y1=$(python3 -c "print(int($wy+$wh-$dh-($ww-$dw)/2+$f1*$dh))")
  y2=$(python3 -c "print(int($wy+$wh-$dh-($ww-$dw)/2+$f2*$dh))")
  cliclick "m:$x,$y1" "dd:$x,$y1" w:60 "m:$x,$(((y1+y2)/2))" w:60 "m:$x,$y2" w:120 "du:$x,$y2"
}

# Whole-screen screenshot, scaled so it can be read at a glance.
# Cropping a guessed region hides what is actually on screen - twice this led to
# a wrong conclusion (a loading state read as "no data", an action sheet read as
# "a hung modal"). Look at the whole screen first.
sim_shot() {
  local out="${1:-/tmp/sim.png}" udid; udid=$(sim_udid)
  xcrun simctl io "$udid" screenshot "$out" >/dev/null 2>&1
  sips -Z "${2:-900}" "$out" --out "${out%.png}-view.png" >/dev/null 2>&1
  echo "${out%.png}-view.png"
}

sim_alive() {
  local udid; udid=$(sim_udid)
  xcrun simctl spawn "$udid" launchctl list 2>/dev/null | grep -c "$BUNDLE_ID" || true
}
