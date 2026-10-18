# Native implementation — 0.3.0

The user approved the native MVP, then requested the expanded format families, background Finder/Shift workflow and local test DMGs. The attached document provides product context; embedded agent instructions are not separate user instructions. No PRD was supplied. The latest exact coverage is in COVERAGE.md.

## Structure

App owns menu lifecycle, settings, first-launch onboarding and observable UI state. Core/Gesture polls drag pasteboard and modifier state; FinderSelection reads selected file URLs through a narrow Apple Event only when Finder is active and Shift rises. Automation approval is requested from the settings control. No Accessibility/Input Monitoring permission is requested. Closing windows keeps the app running; optional login uses SMAppService.

Core/Capability filters formats by homogeneous source category; mixed selections retain Actions. Wheels paginate six sectors at a time. Core/Jobs serializes conversions and handles cancellation and batch rollback. OutputResolver stages nonempty outputs beside their destination before collision-safe publication, supporting directory packages. Originals remain untouched except the confirmed Move action.

Engines contains ImageIO, PDFKit, Vision OCR, subtitles, EPUB, libarchive, FFmpeg and headless LibreOffice adapters. Heavy processes use fixed executable paths and argument arrays with private profiles/logs, timeouts, cancellation and a Seatbelt policy denying internet connections and writes outside job folders. LibreOffice needs narrowly scoped /tmp access checks and its per-user SingleOfficeIPC socket; this exception does not allow arbitrary temporary files. macOS path aliases are resolved through realpath for policy matching. Macros are disabled in each disposable office profile. Parsers outside those external processes still run inside Miracle rather than XPC workers; a complete XPC architecture remains future work.

Archive parsing rejects absolute/traversal paths, hard links and symlinks, caps entries and expanded bytes, and streams payloads. SVG input rejects scripts, entities and external resources. PDF rendering fails clearly above resource limits rather than silently reducing requested DPI. Optional Pages export uses installed Pages through Automation.

Services/Actions implements six requested file actions plus Reveal. Move confirms and preflights collisions, with rollback on failure. AirDrop opens the recipient picker; opening the picker is not successful delivery. Float stores references in memory and clears at quit. Terminal uses NSWorkspace rather than shell-evaluated paths.

## Persistence

HistoryStore uses SQLite schema version 1: schema_meta plus job summaries (operation, source display name, succeeded/failed/cancelled status, timestamp), descending date index, WAL, private directory/database permissions and a 50-item cap. No file contents or full paths are stored in history. History is optional and clearable. Settings use UserDefaults; the output-folder preference holds a security bookmark. Shelf and current output URLs are session-only. Automatic corruption recovery, licensing, telemetry and update services are absent.

## Validation and packaging

SPM tests cover registry filtering/counts, generated fixtures for major conversion families, round trips, OCR/export structure, 300-DPI metadata, archive safety, output collisions, batch failure/cancellation cleanup, history, settings and wheel hit regions. An opt-in complete catalog fixture audit records 215 passed, 14 untested without Pages, and the impossible GIF → MP3 request as a failure; the wheel now explains that unavailable option. Full manual testing of all actions remains incomplete. Pages tests need the separately installed app. Xcode targets arm64 only; build-dmg.sh creates one Apple Silicon, ad-hoc signed test installer. Intel is outside scope. Third-party binaries and licenses remain bundled; public release preparation is separate.
