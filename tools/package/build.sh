#!/usr/bin/env bash
# Build everything a release needs, from the sibling repos, into ../fv-release. Always Release:
# an empty build type means -O0, and finescript runs 16-24x slower.
set -euo pipefail
keystone="$(cd "$(dirname "$0")/../.." && pwd)"
root="$(dirname "$keystone")"
jobs="$(getconf _NPROCESSORS_ONLN)"

for dep in game-language finenet FineStructureVK finegui; do
    cmake -S "$root/$dep" -B "$root/$dep/build" -DCMAKE_BUILD_TYPE=Release \
        -DCMAKE_POLICY_VERSION_MINIMUM=3.5 -DFINESCRIPT_BUILD_TESTS=OFF -DFINENET_BUILD_TESTS=OFF \
        -DFINEVK_BUILD_TESTS=OFF -DFINEVK_BUILD_EXAMPLES=OFF \
        -DFINEGUI_BUILD_TESTS=OFF -DFINEGUI_BUILD_EXAMPLES=OFF
    cmake --build "$root/$dep/build" -j "$jobs"
done

cmake -S "$root/FineStructureVoxel" -B "$root/fv-release" -G Ninja -DCMAKE_BUILD_TYPE=Release \
    -DCMAKE_POLICY_VERSION_MINIMUM=3.5 -DFINEVOX_BUILD_RENDER=ON -DFINEVOX_BUILD_TESTS=OFF
cmake --build "$root/fv-release" --target finevox_run
