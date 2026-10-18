# Changelog

## 0.3.0

- Rename the Apple Silicon app and installer to Miracle.
- Rewrite wheel drag targeting to choose the exact sector under the drop point.
- Add Done, dismiss results after Open/Reveal, stop completed windows floating over other apps, and dismiss successful results after four seconds.
- Trim unused office desktop assets and enforce a DMG download below 270 MB.


## 0.2.0 test build

- Add bundled offline media and office engines, image WebP/AVIF, text-first documents, PDF/OCR exports, spreadsheet, subtitle and archive conversions.
- Add selected Finder file + Shift, one-time onboarding, background menu bar lifecycle and optional Open at Login.
- Add wheel pagination, per-format limitations, terminal app preference and shelf edge pinning.
- Target Apple Silicon only and add test DMG packaging and explicit conversion/tool coverage audits.
- Full 249-conversion / 26-tool coverage remains incomplete; current catalog has 230 canonical pairs and four file tools.

## Earlier Xcode update

- Add a native Xcode app project, shared Miracle scheme, and hosted XCTest target.
- Replace the wheel buttons with rounded annular segments and orange conversion highlighting based on the supplied reference.
- Add a SwiftUI canvas preview for wheel geometry.

## 0.1.0

- Add a native menu bar app with three radial operation menus and a file-picker alternative.
- Add system image conversion, image editing, image-to-PDF, PDF splitting/rendering/text extraction.
- Add a floating file shelf, common file actions, job cancellation and feedback.
- Add output settings, collision-safe publishing, and private local SQLite history.
