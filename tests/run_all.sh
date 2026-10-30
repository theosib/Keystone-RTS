#!/usr/bin/env bash
# Run every headless gameplay scenario. Exit nonzero if any fails.
#
# Each scenario is a real game with a script that asserts on it, so a pass means
# the game logic works end to end, not that a unit passed in isolation.
set -uo pipefail
here="$(cd "$(dirname "$0")/.." && pwd)"
build="${KEYSTONE_BUILD:-$here/../fv-keystone}"
failed=0

run() {
    local name="$1" ticks="$2"
    local output status
    output="$("$build/finevox_headless" --game-root "$here" --ticks "$ticks" \
        --script "$here/tests/$name.fsc" 2>&1)"
    status=$?
    if [ "$status" -eq 0 ]; then
        echo "PASS  $name"
    else
        echo "FAIL  $name (exit $status)"
        echo "$output" | tail -12
        failed=1
    fi
}

run stranded 1000
run inspect 100
run economy 9100
run warehouse 9100
run match 54000
run continue 54000

exit "$failed"
