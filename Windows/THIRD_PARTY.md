# Third-party components

Miracle's own source remains MIT licensed. External components retain their respective licenses; the MIT license does not replace them.

## Managed and image libraries

| Component | Pinned version | License / source |
|---|---|---|
| Magick.NET Q8 and Core | 15.0.0 | Apache-2.0; https://github.com/dlemstra/Magick.NET |
| ImageMagick and its native delegates | Included by Magick.NET | Bundled upstream license notices; https://imagemagick.org/script/license.php |
| PDFsharp-WPF | 6.2.4 | MIT; https://github.com/empira/PDFsharp |
| PDFium native Windows renderer | 157.0.8100 | BSD-style upstream licenses and notices; https://pdfium.googlesource.com/pdfium/ ; build distribution https://github.com/bblanchon/pdfium-binaries |
| PdfPig | 0.1.16 | Apache-2.0; https://github.com/UglyToad/PdfPig |
| Open XML SDK | 3.5.1 | MIT; https://github.com/dotnet/Open-XML-SDK |
| SharpCompress | 1.0.0 | MIT, with notices for incorporated codecs; https://github.com/adamhathcock/sharpcompress |
| .NET / Windows Desktop Runtime | Self-contained runtime resolved by the SDK | MIT and runtime third-party notices; https://github.com/dotnet/runtime |

## Optional local engine pack

- **FFmpeg 9.0.2**, Gyan essentials Windows x64 build. Runs under Windows ARM64 emulation in the development VM. This build is GPLv3; preserve its `LICENSE` and `README.txt`, including build configuration, codec dependencies and corresponding-source information. Vendor: https://www.gyan.dev/ffmpeg/builds/ ; upstream: https://ffmpeg.org/ . Do not apply Miracle's MIT license to FFmpeg.
- **LibreOffice 26.8.1.1**, Windows x64 or ARM64 conversion runtime. Binaries are unmodified; compact distribution omits help, dictionaries, installer database and unused interface resources. Preserve `license.html`, `LICENSE*`, `NOTICE*`, `CREDITS*` and all third-party notices from the image. MPL-2.0 and component-specific licenses apply. Source: https://www.libreoffice.org/about-us/source-code/ . This is a local subprocess engine, not an embedded web service.
- **libheif 1.23.6** and encoder DLLs, from MSYS2 UCRT64 packages. The package lock records official download URLs, SHA-256 digests and individual license identifiers. Preserve `Heif/share/licenses`. Some codecs, including x265, use GPL licenses. Source recipes and patches: https://github.com/msys2/MINGW-packages . libheif: https://github.com/strukturag/libheif . No package manager is needed at runtime.
- **Microsoft Visual C++ ARM64 Redistributable** is a Microsoft-licensed prerequisite, distributed through Microsoft's official download. It is not MIT licensed. Install its official package if required; do not rename or re-sign it as Miracle.

Contributors distributing a complete engine bundle must retain upstream notices and satisfy the corresponding-source obligations of each included license. The app does not download or upload files at runtime.
