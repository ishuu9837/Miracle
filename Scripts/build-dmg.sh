#!/bin/zsh
set -euo pipefail
cd "${0:A:h:h}"
python3 Scripts/trim-office-engine.py
# The PDF-import child must use the same local signing identity as its repackaged parent.
codesign --force --sign - --timestamp=none --options runtime --entitlements Resources/OfficeEngine.entitlements Resources/Engines/arm64/LibreOffice.app/Contents/MacOS/xpdfimport
codesign --force --sign - --timestamp=none --options runtime --entitlements Resources/OfficeEngine.entitlements Resources/Engines/arm64/LibreOffice.app
# A clean product path prevents old names or removed engine resources shipping.
xcodebuild -project Miracle.xcodeproj -scheme Miracle -configuration Release \
  -derivedDataPath .build/miracle-arm64 ARCHS=arm64 ONLY_ACTIVE_ARCH=YES \
  CODE_SIGNING_ALLOWED=NO build
stage="$PWD/.build/miracle-dmg-0.3.0"
app="$stage/Miracle.app"
mkdir -p "$stage" "$PWD/output"
ditto "$PWD/.build/miracle-arm64/Build/Products/Release/Miracle.app" "$app"
[[ "$(lipo -archs "$app/Contents/MacOS/Miracle")" == arm64 ]]
[[ ! -e "$app/Contents/Resources/Engines/x86_64" ]]
codesign --force --sign - --timestamp=none "$app/Contents/Resources/Engines/arm64/ffmpeg"
codesign --force --sign - --timestamp=none --options runtime --entitlements Resources/Miracle.entitlements "$app"
codesign --verify --deep --strict "$app"
ln -sfn /Applications "$stage/Applications"
cp Docs/INSTALL.txt "$stage/Read Me.txt"
cp Docs/THIRD_PARTY.md "$stage/Third-party notices.txt"
image="$PWD/output/miracle.dmg"
hdiutil create -volname miracle -srcfolder "$stage" -ov -format ULMO "$image"
hdiutil verify "$image"
bytes=$(stat -f %z "$image")
if (( bytes >= 270000000 )); then
  print -u2 "DMG is $bytes bytes; exceeds the 270 MB limit."
  exit 1
fi
shasum -a 256 output/miracle.dmg > .build/Miracle-SHA256.txt
print "Installer: $image ($bytes bytes)"
