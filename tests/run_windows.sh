#!/usr/bin/env bash
# Multiplayer in real windows, for what's in the UI: typing, opening a game up, saving. It opens
# several windows for about a minute, so this is separate from run_all.sh. Exit nonzero if any fails.
set -uo pipefail
here="$(cd "$(dirname "$0")/.." && pwd)"
build="${KEYSTONE_BUILD:-$here/../fv-keystone}"
work="$(mktemp -d)"
trap 'pkill -f "mcp-port 789[3-7]" 2>/dev/null; rm -rf "$work"' EXIT
python3 "$here/tests/net/windows.py" "$build" "$here" "$work"
