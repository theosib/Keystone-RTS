# What the packaging scripts share. Sourced, not run.
set -euo pipefail
keystone="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
root="$(dirname "$keystone")"
build="${KEYSTONE_BUILD:-$root/fv-release}"
version="${KEYSTONE_VERSION:-dev}"

# The game as committed, without what's only for working on it. notes.txt and the like are
# untracked, so they can't end up in a release.
stage_game() {
    local dest="$1"
    mkdir -p "$dest"
    git -C "$keystone" archive HEAD | tar -x -C "$dest" \
        --exclude tests --exclude tools --exclude docs --exclude .github --exclude run.sh \
        --exclude CLAUDE.md --exclude README.md --exclude .gitignore --exclude .mcp.json
}

# Compiled shaders, from the build or from SHADERS if CI made them elsewhere: SPIR-V is the same on
# every machine, and Ubuntu 22.04 has no glslc for Arm.
stage_shaders() {
    local dest="$1"
    local from="${SHADERS:-$build/shaders}"
    mkdir -p "$dest"
    cp "$from"/*.spv "$dest"/
    [ -f "$dest/chunk.vert.spv" ] || { echo "no shaders in $from" >&2; exit 1; }
}

# Each licence that goes with what's in the package. A missing one stops the build, so that
# something new can't ship without its notice. Extra ones come as pairs: name, then file.
stage_licenses() {
    local dest="$1"
    shift
    mkdir -p "$dest"
    for repo in Keystone FineStructureVoxel FineStructureVK finegui game-language finenet; do
        cp "$root/$repo/LICENSE" "$dest/$repo-LICENSE"
        cp "$root/$repo/NOTICE" "$dest/$repo-NOTICE"
    done
    cp "$keystone/ui/skin/Almendra-OFL.txt" "$dest/Almendra-OFL.txt"
    cp "$root/FineStructureVK/THIRD_PARTY_LICENSES.md" "$dest/stb-tinyobjloader-LICENSES.md"
    cp "$root/finegui/third_party/imgui/LICENSE.txt" "$dest/imgui-LICENSE.txt"
    cp "$build/_deps/lz4-src/LICENSE" "$dest/lz4-LICENSE"
    cp "$build/_deps/glm-src/copying.txt" "$dest/glm-LICENSE.txt"
    cp "$build/_deps/nlohmann_json-src/LICENSE.MIT" "$dest/nlohmann_json-LICENSE"
    while [ $# -gt 0 ]; do
        cp "$2" "$dest/$1"
        shift 2
    done
}
