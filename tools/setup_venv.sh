#!/usr/bin/env bash
# Create the Python venv used by Keystone's asset tools.
set -euo pipefail
cd "$(dirname "$0")/.."
[ -d .venv ] || python3 -m venv .venv
.venv/bin/pip install -q pillow numpy
