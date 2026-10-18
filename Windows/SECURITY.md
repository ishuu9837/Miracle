# Windows preview security scope

## Checks required before packaging

- Release ARM64 build: warnings treated as errors.
- Real Windows executable suite: conversions and original-byte preservation; collision-safe outputs; malformed/empty/missing inputs; invalid output format/path traversal; subtitle resource bounds and adversarial markup; child-process argument isolation, bounded pipe drains, failures and cancellation; primary form selection/reset/actual conversion; logo intro skip; wheel commitment/close regression.
- NuGet dependency vulnerability audit, including transitive package references.
- Native WPF interface render captured by the Windows executable suite; primary conversion and wheel interactions checked in that suite.
- ZIP integrity check and SHA-256 recorded beside the deliverable.

## Controls

Conversions run locally and do not upload input files. External tools use absolute paths, `UseShellExecute=false`, discrete `ArgumentList` parameters, bounded diagnostic buffers and cancellation of the process tree. FFmpeg is restricted to local file/pipe protocols and has a 30-minute job timeout. Originals are opened read-only; outputs are first written to a unique temporary sibling and committed without overwrite. Partially written files are removed on failure/cancellation. Output formats must come from the detected capability list.

The application requests standard-user privileges and does not alter OS security settings. Explorer keyboard integration is opt-in, stores no keyboard content, and never blocks keyboard events. No external conversion engine is bundled or downloaded automatically. Only select an engine binary you trust.

## Boundaries

A passing suite is evidence for these controls, not a claim that every possible file or vulnerability has been audited. Native image codecs and an optional FFmpeg process execute with the current user's permissions; this preview does not run them in AppContainer isolation. Keep Windows and external engines patched. The portable preview is unsigned. Office/PDF/archives and full Mac parity remain out of scope for this Windows release.
