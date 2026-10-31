#!/usr/bin/env bash
# Check the MCP tools end to end: through the stdio bridge, against a real game. It opens a
# window for a few seconds, so this is separate from run_all.sh. Exit nonzero if any fails.
set -uo pipefail
here="$(cd "$(dirname "$0")/.." && pwd)"
build="${KEYSTONE_BUILD:-$here/../fv-keystone}"
bridge="$here/../FineStructureVoxel/tools/mcp_bridge.py"
check="$here/tests/mcp_check.py"

# Not the default port, so that this can't talk to a game you're playing
export FINEVOX_MCP_PORT=7899
export HOME="$(mktemp -d)"  # The bridge's tool cache
trap 'rm -rf "$HOME"' EXIT
failed=0

run() {
    local mode="$1"
    shift
    echo "---- $mode"
    if [ "$mode" = none ]; then
        python3 "$check" "$bridge" none || failed=1
        return
    fi
    "$@" >"$HOME/$mode.log" 2>&1 &
    local game=$!
    local checked=0
    python3 "$check" "$bridge" "$mode" || checked=1
    if [ "$checked" -eq 0 ] && [ "$mode" = headless ]; then
        wait "$game"
        local status=$?
        if [ "$status" -ne 0 ]; then echo "FAIL headless exit $status"; failed=1; fi
    else
        kill "$game" 2>/dev/null
        wait "$game" 2>/dev/null
    fi
    if [ "$checked" -ne 0 ]; then failed=1; fi
    if grep -q 'ERROR\|\[ui script\]' "$HOME/$mode.log"; then
        echo "FAIL $mode logged errors"
        grep 'ERROR\|\[ui script\]' "$HOME/$mode.log"
        failed=1
    fi
}

run none
rm -f "$HOME/.cache/finevox/"*  # Each mode starts as a session that's never met a game
run headless "$build/finevox_headless" --game-root "$here" --mcp-port "$FINEVOX_MCP_PORT"
rm -f "$HOME/.cache/finevox/"*
run window "$build/finevox_run" --game-root "$here" --new --no-save --seed 7 --size 1280 720 \
    --validation --mcp-port "$FINEVOX_MCP_PORT"
rm -f "$HOME/.cache/finevox/"*
run player "$build/finevox_run" --game-root "$here" --new --no-save --seed 7 --size 1280 720 \
    --mcp-port "$FINEVOX_MCP_PORT" --mcp-player

if [ "$failed" -ne 0 ]; then
    echo "FAIL  mcp"
    exit 1
fi
echo "PASS  mcp"
