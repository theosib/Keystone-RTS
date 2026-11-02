#!/usr/bin/env bash
# Clone the sibling repos next to this one, at the commits PINS names (`dir=sha dir=sha ...`), or
# at the tip of main with none. Prints what it got in the form PINS takes, so that one release can
# build the same code on every machine.
set -euo pipefail
keystone="$(cd "$(dirname "$0")/../.." && pwd)"
root="$(dirname "$keystone")"
pins=" ${PINS:-} "

got=""
for pair in FineStructureVoxel=FineStructureVoxel game-language=FineStructureScript \
            FineStructureVK=FineStructureVulkan finegui=finegui finenet=finenet; do
    dir="${pair%%=*}"
    repo="${pair##*=}"
    sha="$(grep -oE " $dir=[0-9a-f]+ " <<< "$pins" | cut -d= -f2 | tr -d ' ' || true)"
    git clone --quiet "https://github.com/theosib/$repo.git" "$root/$dir" >&2
    [ -z "$sha" ] || git -C "$root/$dir" checkout --quiet "$sha" >&2
    git -C "$root/$dir" submodule update --quiet --init --recursive >&2
    got+="$dir=$(git -C "$root/$dir" rev-parse HEAD) "
done
echo "${got% }"
