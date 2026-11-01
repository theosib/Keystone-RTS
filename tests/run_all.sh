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

# Save a game partway, resume it in a new process, and check it's the same game
resume_scenario() {
    local dir output_a output_b status
    dir="$(mktemp -d)"
    output_a="$("$build/finevox_headless" --game-root "$here" --ticks 2400 \
        --script "$here/tests/resume.fsc" --save "$dir/game" 2>&1)"
    status=$?
    if [ "$status" -ne 0 ]; then
        echo "FAIL  resume, saving (exit $status)"
        echo "$output_a" | tail -8
        failed=1
    else
        output_b="$("$build/finevox_headless" --game-root "$here" --ticks 1800 \
            --script "$here/tests/resume.fsc" --resume "$dir/game" 2>&1)"
        status=$?
        local before after
        before="$(echo "$output_a" | grep SUMMARY)"
        after="$(echo "$output_b" | grep SUMMARY | head -1)"
        if [ "$status" -ne 0 ] || [ -z "$before" ] || [ "$before" != "$after" ]; then
            echo "FAIL  resume (exit $status)"
            echo "  saved:   $before"
            echo "  resumed: $after"
            echo "$output_b" | tail -8
            failed=1
        else
            echo "PASS  resume"
        fi
    fi
    rm -rf "$dir"
}

# Leaves a save already has stranded go on load, and healthy trees are left alone
canopy_resume_scenario() {
    local dir output_a output_b status before after
    dir="$(mktemp -d)"
    output_a="$("$build/finevox_headless" --game-root "$here" --ticks 100 \
        --script "$here/tests/canopy_resume.fsc" --save "$dir/game" 2>&1)"
    output_b="$("$build/finevox_headless" --game-root "$here" --ticks 40 \
        --script "$here/tests/canopy_resume.fsc" --resume "$dir/game" 2>&1)"
    status=$?
    before="$(echo "$output_a" | grep -o 'CANOPY [0-9]*')"
    after="$(echo "$output_b" | grep -o 'CANOPY [0-9]*' | tail -1)"
    if [ "$status" -ne 0 ] || [ -z "$before" ] || [ "$before" != "$after" ]; then
        echo "FAIL  canopy_resume (exit $status): saved '$before', resumed '$after'"
        echo "$output_b" | tail -8
        failed=1
    else
        echo "PASS  canopy_resume"
    fi
    rm -rf "$dir"
}

resume_scenario
canopy_resume_scenario
run stranded 1000
run inspect 100
run canopy 6000
run catalog 20
run fence 6000
run tasks 9000
run economy 9100
run light 9100
run warehouse 9100
run match 54000
run continue 54000
run continue_war 3100
run orders 1000
run explore 27000
run chase 1400
run guard 3200
run deliver 7100
run blueprints 60

if python3 "$here/tools/check_models.py"; then
    echo "PASS  check_models"
else
    echo "FAIL  check_models"
    failed=1
fi

exit "$failed"
