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
