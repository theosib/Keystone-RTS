#!/usr/bin/env bash
# Launch Keystone. Extra arguments go to finevox_run.
#
# A crash leaves a core: lldb -c /cores/core.<pid> ../fv-keystone/finevox_run
# KEYSTONE_CORE=0 turns that off.
set -euo pipefail
here="$(cd "$(dirname "$0")" && pwd)"
build="${KEYSTONE_BUILD:-$here/../fv-keystone}"
bin="$build/finevox_run"

# A core runs 7GB+ even for a trivial program, so keep one and leave room for it
CORE_MIN_FREE_GB=25

enable_core_dumps() {
    if [[ "$(uname)" == Darwin ]]; then
        # macOS skips the core without get-task-allow, and relinking drops the entitlement
        local signature
        signature="$(codesign -d --entitlements - "$bin" 2>/dev/null || true)"
        if [[ "$signature" != *get-task-allow* ]]; then
            codesign -s - -f --entitlements "$here/tools/core-dump.entitlements" "$bin" 2>/dev/null
        fi

        local stale
        stale="$(ls -t /cores 2>/dev/null | tail -n +2)"
        for core in $stale; do
            rm -f "/cores/$core" 2>/dev/null || true
        done

        local free_gb
        free_gb="$(df -g /cores | awk 'NR==2 {print $4}')"
        if (( free_gb < CORE_MIN_FREE_GB )); then
            echo "run.sh: core dumps off, ${free_gb}GB free on /cores (need ${CORE_MIN_FREE_GB}GB)" >&2
            return
        fi
    fi
    ulimit -c unlimited 2>/dev/null || true
}

if [[ "${KEYSTONE_CORE:-1}" != 0 ]]; then
    enable_core_dumps
fi

exec "$bin" --game-root "$here" "$@"
