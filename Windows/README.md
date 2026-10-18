# Miracle for Windows — initial preview

Native C# / WPF port, targeting .NET 10 on Windows 11. This is a separate early implementation: the Mac's full conversion catalog and file actions have not yet been ported.

## Run the portable build

Extract the **entire** ARM64 ZIP in Windows and open `Miracle.exe`. It includes the .NET runtime, so no SDK or runtime installation is needed to run it. Apple Silicon Macs running Windows 11 in Parallels use the **win-arm64** build. Do not copy only the EXE: its companion files are required. This preview is unsigned.

Select files or drop them onto the main window, choose an output format, or open the wheel. Closing the main window keeps Miracle in the tray. Use the tray menu to quit. Optional Shift-tap integration can be enabled in the main window; it reads the selection from a foreground File Explorer window. Desktop selections and automatic login startup are not implemented. Preferences, including the optional shortcut and a manually selected engine path, are session-only for now.

## Current conversion scope

- Native inputs: JPG/JPEG, PNG, BMP, TIFF/TIF, GIF.
- Native image outputs: JPG, PNG, BMP, TIFF. Non-TIFF output uses the first frame; TIFF preserves the decoded image frames. This is a basic raster export, not an animated GIF conversion or metadata preservation tool.
- SRT ↔ VTT, and subtitle → TXT. Basic cue timing and text; advanced WebVTT styling/regions are not preserved.
- Optional FFmpeg: audio MP3, M4A, WAV, FLAC, OGG, Opus, AIFF; WMA input; video MP4, MOV, MKV, WebM, GIF export and MP3 extraction. AVI and WMV input. WebP image routes are also available with FFmpeg.
- Only output codecs detected in the selected FFmpeg build are offered. Input decoding still depends on that build and the particular file. An audio extraction from a silent video fails with an engine error.
- Office/PDF conversion, HEIC/AVIF, archives, and the six file actions are **not yet ported**. LibreOffice is not required for this preview.

Original files are preserved. Outputs use collision-safe `-converted` names. Failed/cancelled unfinished outputs are removed; completed batch outputs are kept. Image jobs can finish their active codec call before cancellation takes effect. FFmpeg jobs have a 30-minute timeout and process-tree cancellation.

## FFmpeg setup (optional)

Obtain a trusted Windows FFmpeg build through the links on https://ffmpeg.org/download.html. Select **Locate ffmpeg.exe** in Miracle, add it to PATH before launching, or put it under `Engines/ffmpeg.exe` beside `Miracle.exe`. Do not use the Mac FFmpeg binary. Review the engine distributor's licenses before redistributing an engine with Miracle. No external engine is included or downloaded automatically in this preview.

## Build

Install the **.NET 10 SDK for Windows ARM64** in a Windows 11 ARM VM, or the x64 SDK on an Intel/AMD Windows PC. Visual Studio Community with **.NET desktop development** is optional; the SDK CLI is sufficient. VS Code alone and extensions named “dotnet” do not install the SDK. Confirm with `dotnet --version` in a new Windows terminal.

```powershell
dotnet build Windows/Miracle.Windows/Miracle.Windows.csproj -c Release
powershell -File Windows/Scripts/publish.ps1 -Runtime win-arm64
```

For x64, use `-Runtime win-x64`. ARM64 has been tested in the development VM; x64 still needs device testing. macOS can cross-compile with `EnableWindowsTargeting`; the WPF application itself runs only on Windows.

## Verification

Run the real Windows conversion checks against the portable build:

```powershell
$p = Start-Process .\Miracle.exe -ArgumentList '--self-test', 'C:\Users\Public\MiracleTest' -Wait -PassThru
Get-Content C:\Users\Public\MiracleTest\report.json
if ($p.ExitCode -ne 0) { throw 'Self-test failed' }
```

Use a dedicated test directory. The report checks native image decoding/encoding, subtitle round-trips, output collisions, cancellation, original preservation, mixed selections, and temporary output cleanup. FFmpeg and Explorer integration require separate manual testing on Windows.
