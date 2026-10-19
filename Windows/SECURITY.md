# Windows preview security scope

## Release gates

1. Complete the conversion table and file-tool checks on Windows. Reopen outputs and verify page, pixel, timing and stream properties. A registered route is not a passing conversion.
2. Run the regression suite, including ordinary-user UI conversion, intro and wheel close, failed/cancelled jobs, malformed inputs, archive paths/links and subprocess isolation.
3. Build with warnings as errors and audit direct/transitive NuGet dependencies.
4. Inspect the **embedded** executable manifest, signature status and standard-user token. Source manifest text alone is not binary verification.
5. Scan the actual app and engine folder with local Windows Defender. Record signature version, enabled status, scan log, detections and result. Exit code zero alone is insufficient: Defender can return zero after remediating a threat.
6. Verify ZIP integrity and SHA-256, then publish the source and matching preview artifact.

`Scripts/check-release.ps1` collects local Defender, Authenticode and policy evidence without changing Windows security settings or submitting artifacts to public multi-engine scanners. The generated report distinguishes observations from unperformed checks.

## Clean-machine reputation checks

The current development VM is **not a clean machine**. Its unsigned preview cannot display a verified publisher. Signature/reputation testing is a separate release gate; a successful local launch or Defender scan does not prove SmartScreen or Smart App Control compatibility.

For a signed public release, sign the actual executable/installer with a trusted code-signing identity, download the final artifact through the intended public distribution URL into a clean Windows test environment, preserve Mark of the Web, and record the displayed publisher and SmartScreen outcome. Test with Smart App Control **enforced**, with the actual shipped engines as well as the launcher. Record the Windows build and control state. Do not disable protections to obtain a pass. A new signed build can still lack reputation.

Primary references: [SmartScreen for developers](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/smartscreen-reputation), [Smart App Control](https://learn.microsoft.com/en-us/windows/apps/develop/smart-app-control/overview), [testing Smart App Control](https://learn.microsoft.com/en-us/windows/apps/develop/smart-app-control/test-your-app-with-smart-app-control), [Defender CLI scan behavior](https://learn.microsoft.com/en-us/defender-endpoint/command-line-arguments-microsoft-defender-antivirus).

## Implemented controls

- Local conversion only; no application cloud conversion or automatic updater. Build scripts download engines separately, with pinned SHA-256 checks. No public scanner upload is part of the application or local release checks.
- External tools receive absolute executable paths, `UseShellExecute=false`, separate `ArgumentList` parameters, bounded stdout/stderr buffers, job timeouts and process-tree cancellation. FFmpeg/ffprobe input protocols are restricted to `file,pipe`.
- Originals are read-only. Each job stages outputs in a random sibling directory, commits without overwriting existing files, and rolls back that job's committed files on failure. Extraction commits a new folder. Previously completed batch jobs remain.
- Restricted image coders and delegates; declared input coders are forced so a disguised vector input cannot invoke the internal MVG renderer. SVG external references/DTDs are rejected. Images have input, pixel and ImageMagick memory/time budgets. HEIC encoding runs through a bounded local subprocess.
- PDF page/render limits, document package/text budgets, XML DTD prohibition and no XML resolver. Office conversion uses an isolated profile with macro security set to its highest level.
- Archive extraction rejects rooted/traversing paths, device names, alternate streams, trailing-dot/space aliases, duplicate case-insensitive paths, links and encrypted entries. Streamed expansion is limited to 2 GB and 10,000 entries.
- Standard-user application manifest (`asInvoker`, `uiAccess=false`). Installing Microsoft's separate C++ runtime prerequisite can require UAC; ordinary conversions do not request elevation.
- Explorer keyboard integration is opt-in, stores no keyboard content, and never blocks keyboard events. No OS security settings are changed by the app.

## Limits

A passing suite is evidence for the tested fixtures and controls, not proof that every file or vulnerability has been audited. Native codecs, PDF rendering and local Office/media processes run with the user's permissions, outside AppContainer isolation. Keep Windows and engines patched. Native image cancellation can wait for the current codec call to finish. This preview is unsigned; clean-machine SmartScreen and enforced Smart App Control passes must not be claimed without separate evidence.
