#!/bin/zsh
set -euo pipefail
cd "${0:A:h:h}"
[[ "$(uname -m)" == arm64 ]] || { print "Miracle targets Apple Silicon Macs only."; exit 1; }
export CLANG_MODULE_CACHE_PATH="${TMPDIR:-/private/tmp}/miracle-clang-cache"
export SWIFTPM_MODULECACHE_OVERRIDE="${TMPDIR:-/private/tmp}/miracle-module-cache"
swift build --disable-sandbox --cache-path "${TMPDIR:-/private/tmp}/miracle-spm-cache" -c release -debug-info-format none -Xswiftc -module-cache-path -Xswiftc "$SWIFTPM_MODULECACHE_OVERRIDE"
app="$PWD/output/Miracle.app"
mkdir -p "$app/Contents/MacOS" "$app/Contents/Resources"
cp .build/release/Miracle "$app/Contents/MacOS/Miracle"
cp Resources/AppIcon.icns Resources/BrandLogo.png "$app/Contents/Resources/"
cp Resources/Info.plist "$app/Contents/Info.plist"
architecture="arm64"
mkdir -p "$app/Contents/Resources/Engines/$architecture"
ditto "Resources/Engines/$architecture" "$app/Contents/Resources/Engines/$architecture"
ditto Resources/Licenses "$app/Contents/Resources/Licenses"
codesign --force --sign - --timestamp=none "$app/Contents/Resources/Engines/$architecture/ffmpeg"
codesign --force --sign - --options runtime --entitlements Resources/Miracle.entitlements "$app"
print "Built $app"
