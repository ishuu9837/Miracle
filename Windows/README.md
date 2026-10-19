# Miracle for Windows — 0.3 preview

Native C# / WPF application for Windows 11, with colorful primary format buttons, an optional conversion wheel and an animated intro using the app logo. The [feature table](FEATURES.md) describes implemented conversions, all 28 category-specific tool entries, and fidelity limits. Release evidence belongs in `Verification/`; implementation alone is not a test pass.

## Run

Extract the **entire** portable ZIP to a local Windows folder and open `Miracle.exe`. The .NET runtime is included. Apple Silicon Macs running Windows 11 in Parallels use **win-arm64**. A Windows x64 build needs x64 Office engines and its own device verification.

Choose files → select a colorful output tile → **Convert**. For crop, trim, metadata, redaction and other operations, choose a file-tool button and enter settings before saving a new result. **Not sure? Use the wheel** remains optional. Outputs are saved beside originals unless you select another folder. Multi-page PDF exports produce one file per page. Use **Show result** to locate the output.

Conversions run locally: no account, upload or cloud conversion service. Closing the main window keeps Miracle in its tray; use the tray menu to quit. Explorer Shift-tap is opt-in, session-only, and reads selected files only while File Explorer is foreground. Desktop selection, automatic login startup and the Mac's six file actions are not implemented in this Windows preview.

## Engines

The application includes local image, PDFium PDF rendering, document, archive and subtitle libraries. HEIC export additionally uses the local `Engines/Heif/bin/heif-enc.exe` engine. FFmpeg/ffprobe handle audio/video. LibreOffice handles legacy documents, slides and sheets. Full Windows packages must include these engines in the layout below, with their notices:

```
Miracle.exe
Engines/
  Media/ffmpeg.exe
  Media/ffprobe.exe
  Heif/bin/heif-enc.exe
  Heif/bin/*.dll
  LibreOffice/program/soffice.com
  LibreOffice/... (complete administrative image)
```

If they are elsewhere, use **Locate ffmpeg.exe** or **Locate LibreOffice**. HEIC uses the bundled path. Office conversion options appear only when LibreOffice is located; media options depend on detected FFmpeg encoders. The official **Microsoft Visual C++ ARM64 Redistributable** is required by native engines. Installing that prerequisite may require administrator approval; Miracle itself requests standard-user privileges.

Build-time preparation downloads official engine packages with SHA-256 checks. It is separate from the application; the application does not download engines. See [THIRD_PARTY.md](THIRD_PARTY.md) for licensing and source information.

## Build and package

Use the .NET 10 SDK. Visual Studio with the .NET desktop workload is optional. VS Code extensions alone do not install the SDK.

```powershell
dotnet build Windows/Miracle.Windows/Miracle.Windows.csproj -c Release -r win-arm64 -p:TreatWarningsAsErrors=true
# Run in Windows to prepare a full ARM64 engine folder:
powershell -File Windows/Scripts/prepare-engines.ps1 -Output C:\MiracleEngines
powershell -File Windows/Scripts/publish.ps1 -Runtime win-arm64 -Engines C:\MiracleEngines
```

Use a PowerShell execution policy permitted by your organization's development environment; no persistent execution-policy change is required by the app. macOS can cross-compile the app, but the application and native Windows tests run on Windows.

## Verify before distribution

```powershell
$p = Start-Process .\Miracle.exe -ArgumentList '--feature-test', 'C:\Users\Public\MiracleFeatureTest' -Wait -PassThru
Get-Content C:\Users\Public\MiracleFeatureTest\feature-report.json
if ($p.ExitCode -ne 0) { throw 'Feature tests failed' }
$p = Start-Process .\Miracle.exe -ArgumentList '--self-test', 'C:\Users\Public\MiracleRegressionTest' -Wait -PassThru
if ($p.ExitCode -ne 0) { throw 'Regression tests failed' }
```

Use a fresh feature-test directory. The suite generates fixtures and actually converts each offered input/output pair, reopens outputs, and runs all file-tool entries. It checks specific page, pixel, timing and stream properties; it does not prove compatibility with every real-world document. The regression suite covers UI selection, intro, wheel close, original preservation and failure/cancellation handling. Final security and distribution checks follow feature verification; see [SECURITY.md](SECURITY.md).

This preview is unsigned. A trusted signing certificate and separate clean-machine SmartScreen / Smart App Control verification are needed for a signed public release. No verified publisher is claimed.
