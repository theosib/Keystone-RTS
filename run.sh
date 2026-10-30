#!/usr/bin/env bash
# Launch Keystone. Extra arguments go to finevox_run.
set -euo pipefail
here="$(cd "$(dirname "$0")" && pwd)"
build="${KEYSTONE_BUILD:-$here/../fv-keystone}"
exec "$build/finevox_run" --game-root "$here" "$@"
