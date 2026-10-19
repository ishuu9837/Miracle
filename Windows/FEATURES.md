# Windows feature implementation — 0.3 preview

All operations run locally. This file describes implemented routes, not a release-test pass. See the generated feature report for the tested build and individual results.

| Category | Inputs | Outputs / actions |
|---|---|---|
| Images | JPG/JPEG, PNG, WebP, HEIC/HEIF, TIFF/TIF, AVIF, BMP, GIF, restricted SVG | JPG, PNG, WebP, HEIC, TIFF, AVIF, BMP, PDF; Compress, Metadata, Edit, Annotate, Add BG, Crop, Redact |
| Video | MP4, MOV, MKV, WebM, AVI, WMV; GIF without audio | MP4, MOV, MKV, WebM, AVI, WMV, GIF, MP3 extraction where audio exists; Compress, Metadata, Mute, Trim, Crop, Speed, Snapshot, Split, Redact |
| Audio | MP3, M4A, WAV, FLAC, OGG, Opus, AIFF, WMA | MP3, M4A, WAV, FLAC, OGG, Opus, AIFF, WMA; Compress, Metadata, Normalize, Visualize, Trim, Channels, Bleep |
| PDF | PDF, up to 500 pages | DOCX, PPTX, XLSX, JPG, PNG, EPUB, TXT, MD; Compress, Metadata, Split, Organize / merge |
| Documents | DOCX, Markdown/MD, TXT, EPUB, HTML; DOC, RTF, ODT through LibreOffice | DOCX, PDF, TXT, MD, HTML, EPUB, PNG, JPG |
| Presentations | PPT, PPTX through LibreOffice | PDF, JPG, PNG |
| Spreadsheets | XLSX, ODS, CSV, TSV; XLS through LibreOffice | XLSX, ODS, CSV, TSV, PDF |
| Subtitles | SRT, VTT | SRT, VTT, TXT |
| Archives | ZIP, TAR, GZIP, RAR input | ZIP, TAR, GZIP (`.tar.gz`); Extract Archive |

Identical input/output formats are not shown as conversions. Audio and video output options are checked against the configured FFmpeg encoders. Office options appear when a local LibreOffice executable is present. Input decoding still depends on the file and engine build.

## Fidelity and scope

- Genuine Apple Pages export is unavailable in this offline Windows version. Use DOCX for exchange with Pages. Apple RTFD bundles and macOS text clippings are not Windows inputs.
- Image conversions and edits use the first image frame. Animated GIF → video and the nine video tools use FFmpeg; GIF inputs also offer first-frame image tools. A GIF has no audio track, so it cannot yield a meaningful MP3.
- PDF → JPG/PNG exports **every page at 300 DPI**, within the 80-megapixel/page and 2-GB/job limits. Slides and documents rendered to images also export all PDF pages.
- PDF → PPTX creates one page image per slide with aspect ratio preserved. PDF → XLSX exports page numbers and extracted text, not reconstructed tables. PDF → DOCX/EPUB/TXT/MD extracts available text; scanned pages need OCR, which this version does not implement.
- Native document conversions reflow text. They do not preserve complex DOCX/EPUB/Markdown formatting. DOCX → PDF/JPG/PNG and legacy Office inputs use LibreOffice when available.
- Image tools save PNG except JPEG compression. Video tools save MP4 except PNG snapshots. Audio tools save WAV except MP3 compression and PNG waveform visualization. Compression does not guarantee a smaller file for every source.
- Bleep replaces the selected time interval with a tone. Image/video redaction permanently masks a pixel rectangle; video redaction keeps audio. Check the output visually before sharing sensitive material.
- Archive conversions repack extracted files, rather than preserving archive-specific metadata. GZIP packaging uses TAR inside GZIP. Extraction rejects traversal paths, links, encrypted entries and duplicate paths; limits are 10,000 entries and 2 GB of expanded file data. Empty directories are not currently preserved when repacking.
- Tool count: **28 category entries** (7 image + 9 video + 7 audio + 4 PDF + 1 archive), with repeated names across categories. This is distinct from a count of unique tool names or conversion pairs.

The Mac build has a separate implementation and verification status. This Windows table is not a claim about Mac coverage.

### Drag + Shift wheel (0.3.2)
With Miracle running, drag a supported selection from File Explorer and tap Shift. Further short taps cycle format pages, then supported file-tool pages, and wrap around. There are at most six colorful choices per set. Keep holding the mouse, slide onto an option, then release. A file-tool drop opens its settings; it does not apply a destructive edit automatically. Esc or dropping outside a choice cancels. Colorful primary buttons remain available. Disable the session shortcut in the app if another utility uses Shift. Desktop/other application drag sources are not supported by this Explorer shortcut.
