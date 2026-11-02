#!/usr/bin/env bash
# Build an AppImage for this machine's architecture. Needs tools/package/build.sh first, on the
# oldest Linux it should run on: that sets the glibc it needs. OUT says where it goes,
# KEYSTONE_VERSION what it's called, GLFW_LICENSE where GLFW's licence is, and LINUXDEPLOY the tool:
# https://github.com/linuxdeploy/linuxdeploy
set -euo pipefail
source "$(dirname "$0")/common.sh"
out="${OUT:-$root/release}"
appdir="$out/AppDir"

rm -rf "${appdir:?}"
mkdir -p "$appdir/usr/bin" "$appdir/usr/share/finevox"

cp "$build/finevox_run" "$appdir/usr/bin/keystone"

stage_game "$appdir/usr/share/finevox/game"
stage_shaders "$appdir/usr/share/finevox/shaders"
stage_licenses "$appdir/usr/share/finevox/licenses" glfw-LICENSE.md "$GLFW_LICENSE"

# libstdc++ and libgcc stay out too. A GPU driver loaded into this process wants the machine's own,
# which may be newer than ours, and anything new enough for our glibc has new enough ones.
cd "$out"
LD_LIBRARY_PATH="$build:$root/game-language/build" LINUXDEPLOY_OUTPUT_VERSION="$version" \
    "$LINUXDEPLOY" --appdir "$appdir" \
    --executable "$appdir/usr/bin/keystone" \
    --desktop-file "$keystone/tools/package/keystone.desktop" \
    --icon-file "$keystone/tools/package/icon-512/keystone.png" \
    --exclude-library 'libstdc++*' --exclude-library 'libgcc_s*' \
    --output appimage

echo "bundled libraries:"
ls "$appdir/usr/lib"
ls "$out"/*.AppImage
