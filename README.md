# Miracle

Open-source, offline file conversion for Apple Silicon Macs. [MIT license](LICENSE).

Native Apple Silicon macOS menu bar file utility built in Swift 6 with SwiftUI and AppKit. macOS 13+. Local conversions use Apple frameworks, bundled FFmpeg and LibreOffice, and system libarchive. Optional Pages export requires installed Apple Pages. No file uploads or runtime engine downloads.

## Open in Xcode

Install Xcode with the macOS SDK and Python 3 for contributor setup. Clone this repository, then fetch the pinned external engines:

```sh
python3 Scripts/fetch-engines.py
python3 Scripts/generate_xcode_project.py
```

The setup script downloads FFmpeg 7.1 and LibreOffice 26.2.6 from their documented upstream sources, verifies SHA-256 before extracting, and puts them in ignored `Resources/Engines/arm64`. This is a build-time download; the running app stays offline. Existing local engine copies are kept. Downloads need roughly 350 MB, and building needs additional disk space.

Open `Miracle.xcodeproj`, choose the Miracle scheme and My Mac, then ⌘R. ⌘U runs the hosted tests. `Scripts/generate_xcode_project.py` regenerates the project from the source folders. The Swift package is also available; `Scripts/test.sh` runs its tests. Bundled engine files under `Resources/Engines` are required for media and office conversions; pinned sources and checksums are in `Docs/THIRD_PARTY.md`.

## Use

Open Miracle once. It stays in the menu bar after its windows close. In Settings → General, enable Finder selection access and approve the macOS Automation prompt. Select a file in Finder, then tap Shift to open its conversion wheel. You can also drag files and tap Shift for Convert, Command for Tools, or Control for Actions. The menu bar's Choose File option works without Finder Automation. Choose an option to run; larger catalogs have Previous/Next pages. Open at Login is optional in Settings.

Outputs use new, collision-safe names. Original files are preserved except through the explicitly confirmed Move action. Cancelled or failed batches remove outputs created by that batch. Float holds file references for this session; the shelf supports pinning to a screen edge and dragging files out.

## Current coverage

The current registry contains **230 canonical conversion pairs**, including DOCX/Pages → PDF. The full generated-fixture audit reports 215 passed, 14 unavailable without Pages, and GIF → MP3 unsupported because GIF contains no audio. Four file tools and all six requested file actions are implemented. **The claimed 249 conversions / 26 tools are not complete.** See [the coverage audit](Docs/COVERAGE.md), [conversion catalog](Docs/CONVERSION_CATALOG.csv), and [tool catalog](Docs/TOOLS_CATALOG.csv).

PDF exports process all pages; images render at 300 DPI within resource limits. PDF → DOCX is text reflow with OCR for scanned pages; PDF → XLSX provides Page/Text columns; PDF → PPTX creates one slide per page. Complex document layouts and embedded media may change. Legacy text clippings may require saving as TXT first. CSV/TSV exports use the first sheet. GZIP packaging creates .tar.gz.

## Test installers

`Scripts/build-dmg.sh` uses Xcode to build an arm64 app and packages `output/miracle.dmg`, with matching bundled engines and an Applications shortcut. Builds are ad-hoc signed and unnotarized. Intel is outside the supported scope. See `Docs/INSTALL.txt`. Public redistribution also needs signing/notarization and completion of bundled-code license/source requirements described in `Docs/THIRD_PARTY.md`.

## Contributing

Run `Scripts/test.sh` for regression tests. The slower complete conversion catalog audit is opt-in:

```sh
MIRACLE_FULL_MATRIX_REPORT="$PWD/Docs/CONVERSION_RESULTS.csv" Scripts/test.sh
```

Build the local test installer with `Scripts/build-dmg.sh`. It trims unused office assets, ad-hoc signs the app and PDF-import helper, verifies the image, and rejects a DMG at or above 270,000,000 bytes. No prebuilt engines, DMGs, signing keys, private requirements document, or local caches are committed.

For changes, include the relevant regression check and update capability documentation when support changes. [Third-party notices](NOTICE.md) distinguish the MIT application source from external programs and vendored materials. Public binary releases still need the signing and corresponding-source work described in Docs/THIRD_PARTY.md.
