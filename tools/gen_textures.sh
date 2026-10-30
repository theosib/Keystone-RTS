#!/usr/bin/env bash
# Regenerate textures/ from tools/gen_textures.py.
set -euo pipefail
cd "$(dirname "$0")/.."
[ -d .venv ] || tools/setup_venv.sh
exec .venv/bin/python tools/gen_textures.py "$@"
