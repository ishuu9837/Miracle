# Third-party notices — Miracle 0.3.0 test builds

Miracle uses bundled external converter executables. They remain separate programs invoked with argument arrays. The application does not upload user files or download engines at runtime.

## FFmpeg 7.1

The Apple Silicon binary comes from the `imageio-ffmpeg` 0.6.0 wheels on PyPI, published by the ImageIO project. Its wheel SHA-256 was checked before extraction.

- Apple Silicon wheel SHA-256: `b1ae3173414b5fc5f538a726c4e48ea97edc0d2cdc11f103afee655c463fa742`
- Package: https://pypi.org/project/imageio-ffmpeg/0.6.0/
- Binary provenance: https://github.com/imageio/imageio-binaries/tree/master/ffmpeg
- FFmpeg source release: https://ffmpeg.org/releases/ffmpeg-7.1.tar.xz
- Upstream source: https://github.com/FFmpeg/FFmpeg/tree/n7.1
- Wrapper source: https://github.com/imageio/imageio-ffmpeg/tree/v0.6.0

This FFmpeg binary is a GPL-enabled build, including libx264, libx265, libvpx, libwebp, libmp3lame, libopus, libvorbis, libaom and other components. The wrapper's BSD license is not the license of the converter binary. GPL texts, the runtime license report and the ARM build configuration are included in `Miracle.app/Contents/Resources/Licenses`.

Before a public distribution, assemble and offer the complete corresponding source and build instructions for the exact FFmpeg builds and linked components, satisfying the applicable GPL terms. Source URLs alone do not establish that all redistribution requirements have been met. These installers are evaluation builds, not a declaration of distribution compliance.

## LibreOffice 26.2.6.3

Official Apple Silicon installer downloaded from The Document Foundation. Program code, filters, fonts and Python are retained; unused help, optional dictionary/extensions and alternate icon packs are removed for headless packaging. The modified outer office bundle and PDF-import helper are ad-hoc signed with their required execution permissions; conversion program code remains intact:

- Apple Silicon: https://download.documentfoundation.org/libreoffice/stable/26.2.6/mac/aarch64/LibreOffice_26.2.6_MacOS_aarch64.dmg
- SHA-256: `94bb3248df074c225490a8a6d1d9dc87c7d6783dbb7a8e9f0d0c3d94348552af`
- Source: https://download.documentfoundation.org/libreoffice/src/26.2.6/
- Licensing: https://www.libreoffice.org/about-us/licenses/

The trimmed headless LibreOffice app is included for offline operation. Its MPL and component licenses/notices remain in its Resources directory; copies of LICENSE and NOTICE are also included in Miracle's Licenses directory. Preserve the licenses, notices and applicable source availability when redistributing.

## macOS libarchive

Miracle links to macOS's system `libarchive`. Public headers are vendored from libarchive v3.7.7, with BSD notices retained. License: `Resources/Licenses/libarchive-COPYING`. Source: https://github.com/libarchive/libarchive/tree/v3.7.7

The small RAR regression fixtures originate from libarchive's test suite under the same upstream licensing. They contain no user data. Unsafe symlink entries are rejected by Miracle.

## Apple frameworks and apps

AppKit, SwiftUI, Foundation, ImageIO, Core Graphics, PDFKit, Vision, ServiceManagement and SQLite are system components. Apple Pages is optional, separately installed software; it is not bundled. Native Pages export uses Apple Events only after macOS authorization.
