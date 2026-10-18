# Coverage audit — 0.2.0 test build

Updated 2026-10-18. Registered capabilities, implementation coverage, and successful end-to-end checks are separate measures. This build does not establish 249 working conversions or 26 completed file tools.

## Conversion catalog

[CONVERSION_CATALOG.csv](CONVERSION_CATALOG.csv) enumerates 230 ordered pairs, excluding aliases and conversions to the same format. It records conditions separately. These totals follow the user's format list plus DOCX and Pages → PDF as reverse office conversions:

| Family | Registered pairs |
|---|---:|
| Images | 50 |
| Audio | 49 |
| Video | 37 |
| PDF | 9 |
| Documents | 45 |
| Slides | 6 |
| Spreadsheets | 21 |
| Subtitles | 4 |
| Archives | 9 |
| Total | 230 |

The original list without those two document → PDF routes totals 228. No distinct set of the remaining 19 pairs has been identified. The 230 registered requests include the conditional Pages routes and the unavailable GIF audio request; this is not 230 working conversions. JPG/JPEG, TIFF/TIF, Markdown/MD and similar aliases are not counted again. Directory packing is additional behavior, not another file-format pair.

The catalog-count test checks every canonical source against the actual native capability registry. A separate full fixture audit now covers every row in [CONVERSION_RESULTS.csv](CONVERSION_RESULTS.csv): **215 passed, 14 unavailable without Pages, 1 failed (GIF → MP3)**. GIF has no audio track, so its MP3 wheel option now explains that extraction is unavailable. There is no fabricated silent MP3 output.

The full audit exported generated fixtures and reopened the results, using media decoding, native image/PDF loading and office package inspection. It records failed routes explicitly. It does not certify arbitrary damaged, encrypted or complex real-world documents. Tests also validate 300-DPI metadata, output rollback and cancellation. Pages routes still need an environment with Apple Pages.

Reproduce the full audit with `MIRACLE_FULL_MATRIX_REPORT="$PWD/Docs/CONVERSION_RESULTS.csv" Scripts/test.sh`. Run `Scripts/audit-capabilities.py` to regenerate the requested catalog; that script counts registrations and does not run conversions.

Pages export needs Apple Pages installed and an Automation approval; Pages is absent on this Mac. PDF office exports are best-effort: DOCX/text outputs reflow text (OCR when needed), XLSX exports Page/Text columns rather than reconstructed tables, PPTX creates one slide per page. Document conversions other than direct office → PDF are text-first. Legacy resource-fork text clippings may require exporting TXT first. CSV/TSV export uses the first spreadsheet sheet. GZIP archive export is .tar.gz. RAR is input-only; unsafe links and paths are rejected.

## File tools

The current Tools wheel implements four tools: image Resize, Compress, Rotate right, and PDF Split pages. Image Create PDF is available in Convert. This is not a completed 26-tool catalog.

The supplied document's catalog contains 30 per-type entries / 20 distinct names. Its statuses are recorded in [TOOLS_CATALOG.csv](TOOLS_CATALOG.csv). No marketing count is used to imply missing tools exist.

## File actions

All six requested actions are implemented: AirDrop, Float, Copy, Move, Copy Path, and Terminal (Terminal/iTerm2 preference). Reveal is an additional convenience. Full manual end-to-end coverage is incomplete; AirDrop delivery requires a selected recipient, and Move requires a destination and confirmation. Implementation is not a claim that every action has passed those manual checks.

## Build and release limits

Apple Silicon is the only supported build and runtime platform; Intel is outside scope. Test DMGs are ad-hoc signed, not Developer ID signed or notarized. See INSTALL.txt and THIRD_PARTY.md for installation and redistribution limitations.
