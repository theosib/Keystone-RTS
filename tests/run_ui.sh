#!/usr/bin/env bash
# Run the scenarios that need a real window: it opens one for a few seconds, so this is separate
# from run_all.sh. Exit nonzero if any fails.
set -uo pipefail
here="$(cd "$(dirname "$0")/.." && pwd)"
build="${KEYSTONE_BUILD:-$here/../fv-keystone}"

root="$(mktemp -d)"
trap 'rm -rf "$root"' EXIT
rsync -a --exclude .git --exclude .venv --exclude saves --exclude screenshots --exclude imgui.ini \
    "$here/" "$root/"
cat "$here/tests/ui_input.fs" >> "$root/ui/main.fs"

output="$(cd "$root" && "$build/finevox_run" --game-root "$root" --new --no-save --seed 7 \
    --size 1280 720 --exit-after 470 --validation --script "$here/tests/ui_step.fsc" 2>&1)"
status=$?

results="$(grep -E '^(PASS|FAIL) ' <<< "$output")"
echo "$results"
if [ "$status" -ne 0 ] || grep -q '^FAIL' <<< "$results" || ! grep -q 'UI TEST DONE' <<< "$output"; then
    echo "FAIL  ui_input (exit $status)"
    tail -12 <<< "$output"
    exit 1
fi
echo "PASS  ui_input"
