# Windows 0.2 verification

Verified on Windows 11 ARM in Parallels on 2026-10-18. The self-contained build uses .NET 10 and passed all 24 executable checks in `windows-arm64.json`, including actual primary-button conversion, wheel commitment and close handling, automatic intro completion/skip, malformed inputs, argument isolation, bounded child output and cancellation.

Release build: compiler warnings treated as errors; zero errors and zero warnings. NuGet audit including transitive package references: no known vulnerable packages reported by the configured NuGet source. ZIP integrity and SHA-256 verified during packaging. No third-party conversion engine is bundled; FFmpeg routes require separate engine verification. See `../SECURITY.md` for scope.
