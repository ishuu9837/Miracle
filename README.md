<div align="center">
  <a href="#miracle">
    <img src="assets/readme/hero-banner.svg" width="100%" alt="Miracle — Open-source, offline file conversion for Apple Silicon Macs. Native Swift 6 menu bar utility.">
  </a>
</div>

<p align="center">
  <img src="https://img.shields.io/badge/Platform-macOS_13%2B-1C2331?style=for-the-badge&labelColor=0A0B0E&logoColor=E5D0B1&logo=apple" alt="macOS 13+">
  <img src="https://img.shields.io/badge/Arch-Apple_Silicon_(arm64)-1C2331?style=for-the-badge&labelColor=0A0B0E&logoColor=E5D0B1&logo=apple" alt="Apple Silicon arm64">
  <img src="https://img.shields.io/badge/Swift-6.0-1C2331?style=for-the-badge&labelColor=0A0B0E&logoColor=E5D0B1&logo=swift" alt="Swift 6">
  <img src="https://img.shields.io/badge/Mode-100%25_Offline-1C2331?style=for-the-badge&labelColor=0A0B0E&logoColor=E5D0B1&logo=shield" alt="100% Offline">
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-MIT-3A3228?style=for-the-badge&labelColor=0A0B0E&logoColor=F4F6F8" alt="MIT License"></a>
</p>

<!-- 01 OVERVIEW -->
<img src="assets/readme/title-overview.svg" width="100%" alt="01 Overview — Local power. Zero cloud.">
<img src="assets/readme/card-overview.svg" width="100%" alt="Native Apple Silicon macOS menu bar file utility built in Swift 6 with SwiftUI and AppKit. Local conversions use Apple frameworks, bundled FFmpeg 7.1 and LibreOffice 26.2.6, and system libarchive. Optional Pages export requires installed Apple Pages. No file uploads or runtime engine downloads.">

<!-- 02 WORKFLOW & USE -->
<img src="assets/readme/title-workflow.svg" width="100%" alt="02 Interaction — Select. Tap Shift. Done.">
<img src="assets/readme/card-workflow.svg" width="100%" alt="Workflow: Select a file in Finder and tap Shift to open the conversion wheel. Drag files and tap Shift for Convert, Command for Tools, or Control for Actions. Float shelf holds session references and pins to screen edges. Collision-safe naming and atomic batch cleanup.">

<!-- 03 COVERAGE & CAPABILITIES -->
<img src="assets/readme/title-coverage.svg" width="100%" alt="03 Coverage — Verified conversion matrix.">
<img src="assets/readme/card-coverage.svg" width="100%" alt="Current registry: 230 canonical conversion pairs (215 passed generated-fixture audit, 14 optional with Apple Pages, 1 unsupported GIF to MP3). 4 file tools and 6 file actions implemented. Note: The claimed 249 conversions / 26 tools are not yet complete.">

<p align="center">
  <a href="Docs/COVERAGE.md"><img src="https://img.shields.io/badge/Audit_Report-Docs%2FCOVERAGE.md-1C2331?style=for-the-badge&labelColor=0A0B0E&logoColor=E5D0B1&logo=readthedocs" alt="Coverage Audit"></a>
  <a href="Docs/CONVERSION_CATALOG.csv"><img src="https://img.shields.io/badge/Catalog-230_Conversion_Pairs-1C2331?style=for-the-badge&labelColor=0A0B0E&logoColor=E5D0B1&logo=databricks" alt="Conversion Catalog"></a>
  <a href="Docs/TOOLS_CATALOG.csv"><img src="https://img.shields.io/badge/Tools-Tools_Catalog-1C2331?style=for-the-badge&labelColor=0A0B0E&logoColor=E5D0B1&logo=serverless" alt="Tools Catalog"></a>
</p>

<!-- 04 ARCHITECTURE & ENGINES -->
<img src="assets/readme/title-engines.svg" width="100%" alt="04 Architecture — Pinned, verified, bundled.">
<img src="assets/readme/card-engines.svg" width="100%" alt="Tech Stack & Engines: Swift 6, SwiftUI, AppKit, Apple Frameworks, FFmpeg 7.1 (SHA-256 verified), LibreOffice 26.2.6 (SHA-256 verified), system libarchive, and optional Apple Pages automation. PDF exports at 300 DPI, OCR reflow for PDF to DOCX, sheet extraction, and tar.gz packaging.">

<!-- 05 BUILD & XCODE -->
<img src="assets/readme/title-build.svg" width="100%" alt="05 Quickstart — Open in Xcode.">

```sh
# 1. Fetch pinned external engines (~350 MB, SHA-256 verified into Resources/Engines/arm64)
python3 Scripts/fetch-engines.py

# 2. Generate Xcode project from source folders
python3 Scripts/generate_xcode_project.py
```

> **Build & Run:** Open `Miracle.xcodeproj`, choose the **Miracle** scheme and **My Mac**, then press `⌘R` (`⌘U` runs hosted tests). Engine downloads happen strictly at build time—the running app stays 100% offline. Existing local engine copies in `Resources/Engines/arm64` are preserved.

<!-- 06 INSTALLERS & CONTRIBUTING -->
<img src="assets/readme/title-contributing.svg" width="100%" alt="06 Packaging & QA — Reproducible builds and matrix audits.">
<img src="assets/readme/card-contributing.svg" width="100%" alt="Testing & Packaging: Run Scripts/test.sh for regression checks. Build output/miracle.dmg via Scripts/build-dmg.sh (trims unused office assets, ad-hoc signs app & PDF helper, enforces <270 MB DMG cap). Intel is outside supported scope.">

```sh
# Run standard Swift package & hosted regression tests
Scripts/test.sh

# Run opt-in complete conversion catalog matrix audit
MIRACLE_FULL_MATRIX_REPORT="$PWD/Docs/CONVERSION_RESULTS.csv" Scripts/test.sh

# Build local ad-hoc signed arm64 test installer (output/miracle.dmg, <270 MB limit)
Scripts/build-dmg.sh
```

<p align="center">
  <a href="Docs/INSTALL.txt"><img src="https://img.shields.io/badge/Installer_Guide-Docs%2FINSTALL.txt-1C2331?style=for-the-badge&labelColor=0A0B0E&logoColor=E5D0B1&logo=apple" alt="Install Guide"></a>
  <a href="Docs/THIRD_PARTY.md"><img src="https://img.shields.io/badge/Checksums_%26_Sources-Docs%2FTHIRD__PARTY.md-1C2331?style=for-the-badge&labelColor=0A0B0E&logoColor=E5D0B1&logo=git" alt="Third Party Docs"></a>
  <a href="NOTICE.md"><img src="https://img.shields.io/badge/Notices-NOTICE.md-1C2331?style=for-the-badge&labelColor=0A0B0E&logoColor=E5D0B1&logo=bookstack" alt="Third-party notices"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-MIT-3A3228?style=for-the-badge&labelColor=0A0B0E&logoColor=F4F6F8" alt="MIT License"></a>
</p>

<img src="assets/readme/footer-signature.svg" width="100%" alt="Miracle✨ — Native Apple Silicon file conversion. Built to stay on your Mac.">
