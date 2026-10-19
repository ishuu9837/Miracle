# Miracle for Windows — 0.3 preview

Native C# / WPF application for Windows 11, with colorful primary format buttons, an optional conversion wheel and an animated intro using the app logo. The [feature table](FEATURES.md) describes implemented conversions, all 28 category-specific tool entries, and fidelity limits. Release evidence belongs in `Verification/`; implementation alone is not a test pass.

## Run

Choose **x64 for Intel/AMD Windows PCs** or **ARM64 for Snapdragon PCs and Windows ARM virtual machines**. Extract the entire `.7z` portable package into a local folder, then open `Miracle/Miracle.exe`. Windows 11 24H2 or later can extract it with **Extract All**; an updated 7-Zip also works. The .NET runtime and offline engines are included. Download size and verification scope are recorded for each architecture in the release. The expanded app needs more disk space than its compressed download.

Choose files → select a colorful output tile → **Convert**. For crop, trim, metadata, redaction and other operations, choose a file-tool button and enter settings before saving a new result. **Not sure? Use the wheel** remains optional. Outputs are saved beside originals unless you select another folder. Multi-page PDF exports produce one file per page. Use **Show result** to locate the output.

Conversions run locally: no account, upload or cloud conversion service. Closing the main window keeps Miracle in its tray; use the tray menu to quit. Explorer Shift is enabled while Miracle is running and can be turned off in the app. While dragging files in File Explorer, tap Shift once to open a nonactivating wheel; later short taps cycle six-option sets of common formats and file tools. Keep holding the mouse button, slide onto a colored option and release. Format drops convert; tool drops open settings first. Escape cancels, and releasing outside an option dismisses the drag wheel. Shift-tapping an Explorer selection without dragging also opens the wheel. The hook does not swallow keys or record keyboard content; it ignores injected events and modified/long Shift holds. Desktop selection, automatic login startup and the Mac's six file actions are not implemented in this Windows preview.

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
  LibreOffice/... (compact headless conversion runtime)
```

If they are elsewhere, use **Locate ffmpeg.exe** or **Locate LibreOffice**. HEIC uses the bundled path. Office conversion options appear only when LibreOffice is located; media options depend on detected FFmpeg encoders. The matching official **Microsoft Visual C++ x64 or ARM64 Redistributable** is required by native engines. Installing that prerequisite may require administrator approval; Miracle itself requests standard-user privileges.

Build-time preparation downloads official engine packages with SHA-256 checks. It is separate from the application; the application does not download engines. See [THIRD_PARTY.md](THIRD_PARTY.md) for licensing and source information.

## Build and package

Use the .NET 10 SDK. Visual Studio with the .NET desktop workload is optional. VS Code extensions alone do not install the SDK.

```powershell
dotnet build Windows/Miracle.Windows/Miracle.Windows.csproj -c Release -r win-x64 -p:TreatWarningsAsErrors=true
# Install official 7-Zip on the build machine, then prepare x64 engines:
powershell -File Windows/Scripts/prepare-engines.ps1 -Runtime win-x64 -Output C:\MiracleEngines
powershell -File Windows/Scripts/publish.ps1 -Runtime win-x64 -Engines C:\MiracleEngines -Prerequisite C:\Downloads\vc_redist.x64.exe
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

## Compact distribution

The compact package preserves conversion binaries, document filters, Unicode data, fonts and upstream licenses. It omits Office spell-check dictionaries, translated interface resources (English fallback retained), offline help, the MSI installer database, alternative Office UI icon themes (Colibre retained) and .NET localized UI resources. These extras are not used for the supported headless conversions. This is an English app interface; Unicode document content remains supported by the retained fonts and conversion engines.

Solid LZMA2 compression produces a `.7z` download; the build script refuses an archive of 300,000,000 bytes or larger. This limit applies to the download, not the extracted app. No conversion engines are downloaded at runtime. See the release verification for tests against the compact package and whether x64 was tested on native hardware or under Windows ARM emulation.
