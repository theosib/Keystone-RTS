#!/usr/bin/env bash
# Build Keystone.app and a DMG of it, with an ad-hoc signature. Needs tools/package/build.sh first.
# OUT says where they go, KEYSTONE_VERSION what they're called. MoltenVK and the Vulkan loader are
# Homebrew's unless MOLTENVK_DYLIB, MOLTENVK_LICENSE and VULKAN_LICENSE say otherwise.
set -euo pipefail
source "$(dirname "$0")/common.sh"
out="${OUT:-$root/release}"
app="$out/Keystone.app"
contents="$app/Contents"
arch="$(uname -m)"

rm -rf "${app:?}"
mkdir -p "$contents/MacOS" "$contents/Resources/vulkan/icd.d"

cp "$build/finevox_run" "$contents/MacOS/Keystone"
chmod u+w "$contents/MacOS/Keystone"

stage_game "$contents/Resources/game"
stage_shaders "$contents/Resources/shaders"

# The Vulkan loader finds the driver in a bundle by this file
moltenvk="${MOLTENVK_DYLIB:-$(brew --prefix molten-vk)/lib/libMoltenVK.dylib}"
cat > "$contents/Resources/vulkan/icd.d/MoltenVK_icd.json" <<JSON
{
    "file_format_version": "1.0.0",
    "ICD": {
        "library_path": "../../../Frameworks/libMoltenVK.dylib",
        "api_version": "1.3.0",
        "is_portability_driver": true
    }
}
JSON

minos="$(python3 "$keystone/tools/package/bundle_dylibs.py" "$contents/MacOS/Keystone" \
    "$contents/Frameworks" "$moltenvk")"

iconset="$out/keystone.iconset"
rm -rf "${iconset:?}"
mkdir -p "$iconset"
for size in 16 32 128 256 512; do
    sips -z "$size" "$size" "$keystone/tools/package/keystone.png" \
        --out "$iconset/icon_${size}x${size}.png" >/dev/null
    double=$((size * 2))
    sips -z "$double" "$double" "$keystone/tools/package/keystone.png" \
        --out "$iconset/icon_${size}x${size}@2x.png" >/dev/null
done
iconutil -c icns "$iconset" -o "$contents/Resources/keystone.icns"
rm -rf "${iconset:?}"

sed -e "s/@VERSION@/$version/g" -e "s/@MINOS@/$minos/g" \
    "$keystone/tools/package/Info.plist.in" > "$contents/Info.plist"

stage_licenses "$contents/Resources/licenses" \
    glfw-LICENSE.md "$(brew --prefix glfw)/LICENSE.md" \
    MoltenVK-LICENSE "${MOLTENVK_LICENSE:-$(brew --prefix molten-vk)/LICENSE}" \
    vulkan-loader-LICENSE.txt "${VULKAN_LICENSE:-$(brew --prefix vulkan-loader)/LICENSE.txt}"

# Inside out: signing a bundle needs what's in it signed already. Apple silicon won't run native
# code with no signature, and everything bundle_dylibs.py touched has lost its own.
for library in "$contents"/Frameworks/*.dylib; do
    codesign --force --sign - "$library"
done
codesign --force --sign - --identifier io.github.theosib.keystone "$app"
codesign --verify --deep --strict "$app"

links="$(otool -L "$contents/MacOS/Keystone" "$contents"/Frameworks/*.dylib)"
if grep -E '^\s+(/opt/|/usr/local|/Users/)' <<< "$links"; then
    echo "a library still points outside the app" >&2
    exit 1
fi

dmg="$out/Keystone-$version-macOS-$arch.dmg"
volume="$out/dmg"
rm -rf "${volume:?}"
rm -f "$dmg"
mkdir -p "$volume"
cp -R "$app" "$volume/"
ln -s /Applications "$volume/Applications"
hdiutil create -volname Keystone -srcfolder "$volume" -fs HFS+ -format UDZO "$dmg" >/dev/null
rm -rf "${volume:?}"
echo "$dmg (needs macOS $minos)"
