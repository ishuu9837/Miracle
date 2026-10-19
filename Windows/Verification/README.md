# Windows 0.3.1 compact releases

Both offline downloads are strictly below **300,000,000 bytes**. Exact hashes and sizes are in [`v0.3.1-compact/`](v0.3.1-compact/).

| Download | Architecture | Compressed size | Expanded file bytes |
|---|---|---:|---:|
| Windows x64 | Intel/AMD AMD64 | 275,716,541 bytes | 1,286,503,738 bytes |
| Windows ARM64 | Snapdragon / Windows ARM VMs | 240,841,354 bytes | 1,296,134,798 bytes |

- x64: **303/303 feature checks and 46/46 ordinary-user regression checks passed**, both processes exited zero. Tested as an AMD64 app under Windows 11 ARM64 emulation in Parallels, not native Intel/AMD hardware. The final x64-default source configuration rebuilt byte-identical executable and app assembly files.
- ARM64: compact Office catalog checks passed (102 executed cases, with overlapping fixtures), two DOCX all-page/300-DPI export checks, nine GIF video-tool checks, and the final 46-check regression. Office checks preceded the isolated startup-deadline/version update; page, media and regression checks used 0.3.1. These are fixture/check counts, not unique conversion-pair counts.
- Windows built-in `tar.exe` extracted both `.7z` archives. Every SHA-256 matched the corresponding stage: **9,464 x64 files and 9,517 ARM64 files**, no additional files. Independent 7-Zip integrity tests also passed.
- Local Defender scans of the exact extracted packages, including every engine, completed with exit zero and explicit **found no threats** logs. Antivirus and real-time protection were enabled, signatures **1.461.91.0**. No public scanner upload.
- Embedded executable manifests: `asInvoker`, `uiAccess=false`. Both are unsigned. Smart App Control was Evaluation, not Enforced; clean-machine SmartScreen, verified publisher and enforced SAC checks remain unverified.
- Builds used warnings as errors. Explicit x64 direct/transitive NuGet audit at low severity and above completed without warnings; the package set records the x64 Magick.NET variant. This audit does not cover native engine source code.

The previous x64 test attempt exposed a cold media-engine probe timeout. Version 0.3.1 allows a bounded 30 seconds for antivirus/emulation startup before treating that engine as unavailable. The final full suite includes successful media generation and conversions.

Compaction removes Office help, dictionaries, installer database, unused interface translations/themes and .NET localized UI resources, while retaining conversion binaries/filters, fonts, Unicode data and licenses. The app interface uses English. No engine is fetched at runtime. The compressed download limit does not apply to the expanded app or conversion workspace. Windows 11 24H2+ supports native extraction; an updated 7-Zip also works.

Unnotarized/unsigned previews and fixture-based tests do not prove universal fidelity or absence of vulnerabilities. See [FEATURES](../FEATURES.md) and [SECURITY](../SECURITY.md) for limitations. The Mac build is separate.

---

# Windows 0.3 ARM64 preview verification

Collected on 2026-10-19 in a Windows 11 ARM64 Parallels development VM, build 26300. Evidence: [`v0.3-arm64/`](v0.3-arm64/). This VM is not a clean-machine reputation test.

- **303/303 feature checks passed**, with output reopening and pixel/page/timing/stream assertions. Process exit zero after replacing the WinRT PDF renderer with explicit-lifecycle PDFium.
- The subsequent Office temporary-path fix passed **38 Office cases** and **2 all-page DOCX image export checks at 300 DPI**. Nine animated-GIF video tools passed separately. These overlapping checks are not additional unique conversion pairs.
- Final ordinary-user regression: **46/46 passed**, exit zero. Includes primary-button conversion, intro completion/skip, a nonoverlapping 14-option wheel/commit/close, malformed inputs, budgets, archive traversal/collisions/links, process argument isolation/output bounds/cancellation and original preservation.
- Release compilation used warnings as errors with no warnings or errors. Direct/transitive NuGet audit reported no known vulnerable packages from the configured source; native engine code is outside that audit.
- Final embedded manifest: **asInvoker**, **uiAccess=false**. Ordinary-user token: medium integrity, not administrator. The separate official C++ runtime prerequisite can require UAC.
- Local Defender custom scan of the final app and all physical engine files completed with exit zero and explicit **found no threats** log. Antivirus and real-time protection enabled; signatures **1.461.91.0**. No public scanner submission.
- ZIP integrity passed for **20,124 files**. Download: **710,181,298 bytes**. SHA-256 is recorded in `artifact.json` and the release sidecar.

## Remaining public-release gates

The executable is **unsigned** and cannot show a verified publisher. Smart App Control was in **Evaluation**, not Enforced. Clean-machine public-download SmartScreen and enforced Smart App Control results are **unverified**. No protections were disabled to obtain a pass. This download is a preview, not a signed public-release certification.

The final executable has matching SHA-256 values in embedded-manifest and release-security reports. The broad report covers the preceding build; targeted Office/page checks and the final regression cover the isolated changes since then. Reports contain synthetic fixture paths. Tests demonstrate these fixtures and controls, not universal fidelity or absence of vulnerabilities.

See [`FEATURES.md`](../FEATURES.md) for text-first PDF/document limitations, no OCR and no Apple Pages export. ARM64 only; Intel/AMD x64 binaries are not released or hardware-tested.

---

# Windows 0.2 verification

Verified on Windows 11 ARM in Parallels on 2026-10-18. The self-contained build uses .NET 10 and passed all 24 executable checks in `windows-arm64.json`, including actual primary-button conversion, wheel commitment and close handling, automatic intro completion/skip, malformed inputs, argument isolation, bounded child output and cancellation.

Release build: compiler warnings treated as errors; zero errors and zero warnings. NuGet audit including transitive package references: no known vulnerable packages reported by the configured NuGet source. ZIP integrity and SHA-256 verified during packaging. No third-party conversion engine is bundled; FFmpeg routes require separate engine verification. See `../SECURITY.md` for scope.
