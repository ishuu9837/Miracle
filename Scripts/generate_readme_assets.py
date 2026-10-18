#!/usr/bin/env python3
"""
Generates custom LED Dot-Matrix SVG banners and Bento Cards for Miracle's README.md
matching the obsidian, warm champagne gold, and slate-blue halftone theme.
"""

from pathlib import Path

OUT_DIR = Path("assets/readme")
OUT_DIR.mkdir(parents=True, exist_ok=True)

COMMON_DEFS = """
  <defs>
    <linearGradient id="bgGrad" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#07080A"/>
      <stop offset="50%" stop-color="#0D1017"/>
      <stop offset="100%" stop-color="#080A0F"/>
    </linearGradient>
    <radialGradient id="goldGlow" cx="25%" cy="20%" r="60%">
      <stop offset="0%" stop-color="#E5D0B1" stop-opacity="0.22"/>
      <stop offset="50%" stop-color="#6482A6" stop-opacity="0.08"/>
      <stop offset="100%" stop-color="#07080A" stop-opacity="0"/>
    </radialGradient>
    <radialGradient id="blueGlow" cx="85%" cy="75%" r="55%">
      <stop offset="0%" stop-color="#7494B8" stop-opacity="0.20"/>
      <stop offset="55%" stop-color="#E5D0B1" stop-opacity="0.06"/>
      <stop offset="100%" stop-color="#07080A" stop-opacity="0"/>
    </radialGradient>
    <linearGradient id="borderGrad" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#E5D0B1" stop-opacity="0.35"/>
      <stop offset="50%" stop-color="#6482A6" stop-opacity="0.25"/>
      <stop offset="100%" stop-color="#E5D0B1" stop-opacity="0.15"/>
    </linearGradient>
    <pattern id="dotMatrix" width="10" height="10" patternUnits="userSpaceOnUse">
      <circle cx="5" cy="5" r="1.6" fill="#ffffff" fill-opacity="0.07"/>
    </pattern>
    <pattern id="dotMatrixBright" width="10" height="10" patternUnits="userSpaceOnUse">
      <circle cx="5" cy="5" r="2.1" fill="#E5D0B1" fill-opacity="0.16"/>
    </pattern>
    <style>
      .font-sans { font-family: -apple-system, BlinkMacSystemFont, 'SF Pro Display', 'Inter', 'Segoe UI', sans-serif; }
      .font-mono { font-family: 'SF Mono', 'JetBrains Mono', 'Menlo', monospace; }
      .kicker { font-size: 12px; font-weight: 700; fill: #E5D0B1; letter-spacing: 2.5px; text-transform: uppercase; }
      .title { font-size: 24px; font-weight: 700; fill: #F4F6F8; letter-spacing: -0.4px; }
      .subtitle { font-size: 15px; font-weight: 400; fill: #9BA6B2; }
      .card-title { font-size: 16px; font-weight: 700; fill: #F4F6F8; }
      .card-body { font-size: 13.5px; font-weight: 400; fill: #9BA6B2; }
      .stat-num { font-size: 32px; font-weight: 800; fill: #E5D0B1; }
      .stat-label { font-size: 12px; font-weight: 600; fill: #9BA6B2; letter-spacing: 0.8px; text-transform: uppercase; }
      .pill { font-size: 11.5px; font-weight: 600; fill: #E5D0B1; }
    </style>
  </defs>
"""

def write_svg(filename: str, width: int, height: int, body: str):
    svg = f"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {width} {height}" width="100%">
{COMMON_DEFS}
  <rect width="{width}" height="{height}" rx="16" fill="url(#bgGrad)"/>
  <rect width="{width}" height="{height}" rx="16" fill="url(#goldGlow)"/>
  <rect width="{width}" height="{height}" rx="16" fill="url(#blueGlow)"/>
  <rect width="{width}" height="{height}" rx="16" fill="url(#dotMatrix)"/>
  <rect x="1" y="1" width="{width-2}" height="{height-2}" rx="15" fill="none" stroke="url(#borderGrad)" stroke-width="1.2"/>
{body}
</svg>"""
    (OUT_DIR / filename).write_text(svg.strip() + "\n", encoding="utf-8")

def make_section_title(filename: str, num: str, category: str, headline: str):
    body = f"""
  <g class="font-sans" transform="translate(32, 0)">
    <rect x="0" y="24" width="38" height="22" rx="6" fill="#181D27" stroke="#E5D0B1" stroke-opacity="0.4" stroke-width="1"/>
    <text x="19" y="39" text-anchor="middle" class="font-mono" font-size="11" font-weight="700" fill="#E5D0B1">{num}</text>
    <text x="50" y="39" class="kicker">{category}</text>
    <text x="0" y="72" class="title">{headline}</text>
  </g>
"""
    write_svg(filename, 1000, 96, body)

# 0. HERO BANNER
hero_body = """
  <circle cx="220" cy="70" r="180" fill="url(#dotMatrixBright)" opacity="0.7"/>
  <circle cx="820" cy="240" r="190" fill="url(#dotMatrixBright)" opacity="0.6"/>
  <g class="font-sans" text-anchor="middle">
    <text x="500" y="145" font-size="76" font-weight="800" fill="#FFFFFF" letter-spacing="-2.5">miracle</text>
    <!-- Four-point sparkles matching uploaded logo -->
    <path d="M 655 68 Q 655 95 682 95 Q 655 95 655 122 Q 655 95 628 95 Q 655 95 655 68 Z" fill="#FFFFFF"/>
    <path d="M 684 56 Q 684 67 695 67 Q 684 67 684 78 Q 684 67 673 67 Q 684 67 684 56 Z" fill="#E5D0B1"/>
    <text x="500" y="195" font-size="18" font-weight="500" fill="#E5D0B1" letter-spacing="0.4">Open-source, offline file conversion for Apple Silicon Macs</text>
    <text x="500" y="226" font-size="14" fill="#9BA6B2">Native Swift 6 menu bar utility · Bundled FFmpeg 7.1 &amp; LibreOffice 26.2.6 · Zero cloud uploads</text>
  </g>
"""
write_svg("hero-banner.svg", 1000, 280, hero_body)

# SECTION HEADERS
make_section_title("title-overview.svg", "01", "Overview", "Local power. Zero cloud.")
make_section_title("title-workflow.svg", "02", "Interaction", "Select in Finder. Tap Shift. Convert.")
make_section_title("title-coverage.svg", "03", "Coverage Matrix", "Audited conversions, tools, and actions.")
make_section_title("title-engines.svg", "04", "Architecture", "Pinned engines. Verified SHA-256. Strictly offline.")
make_section_title("title-build.svg", "05", "Developer Setup", "Open in Xcode and build from source.")
make_section_title("title-contributing.svg", "06", "Packaging & QA", "Reproducible DMG installers and full-matrix audits.")

# CARD 01: OVERVIEW
overview_body = """
  <g class="font-sans" transform="translate(32, 32)">
    <rect x="0" y="0" width="455" height="156" rx="12" fill="#0E121A" fill-opacity="0.85" stroke="#263042"/>
    <text x="24" y="34" class="kicker">Native macOS Architecture</text>
    <text x="24" y="64" class="card-title">Swift 6 + SwiftUI + AppKit Menu Bar Utility</text>
    <text x="24" y="92" class="card-body">Designed exclusively for Apple Silicon (arm64) on macOS 13+.</text>
    <text x="24" y="114" class="card-body">Stays quietly in the menu bar after windows close with optional</text>
    <text x="24" y="136" class="card-body">Open at Login and zero background telemetry.</text>

    <rect x="481" y="0" width="455" height="156" rx="12" fill="#0E121A" fill-opacity="0.85" stroke="#263042"/>
    <text x="505" y="34" class="kicker">Air-Gapped Execution</text>
    <text x="505" y="64" class="card-title">No File Uploads or Runtime Engine Downloads</text>
    <text x="505" y="92" class="card-body">Powered locally by Apple frameworks, system libarchive, and</text>
    <text x="505" y="114" class="card-body">bundled arm64 binaries for FFmpeg 7.1 and LibreOffice 26.2.6.</text>
    <text x="505" y="136" class="card-body">Optional Pages export integrates with installed Apple Pages.</text>
  </g>
"""
write_svg("card-overview.svg", 1000, 220, overview_body)

# CARD 02: WORKFLOW
workflow_body = """
  <g class="font-sans" transform="translate(32, 32)">
    <rect x="0" y="0" width="298" height="170" rx="12" fill="#0E121A" fill-opacity="0.85" stroke="#263042"/>
    <text x="22" y="32" class="kicker">01 · Trigger</text>
    <text x="22" y="60" class="card-title">Finder Wheel &amp; Hotkeys</text>
    <text x="22" y="88" class="card-body">Select in Finder + tap Shift for the</text>
    <text x="22" y="108" class="card-body">conversion wheel. Or drag files and</text>
    <text x="22" y="128" class="card-body">tap Shift (Convert), ⌘ (Tools), or</text>
    <text x="22" y="148" class="card-body">⌃ (Actions). Menu bar file picker too.</text>

    <rect x="319" y="0" width="298" height="170" rx="12" fill="#0E121A" fill-opacity="0.85" stroke="#263042"/>
    <text x="341" y="32" class="kicker">02 · Session Shelf</text>
    <text x="341" y="60" class="card-title">Float Shelf &amp; Edge Pinning</text>
    <text x="341" y="88" class="card-body">Float holds file references for your</text>
    <text x="341" y="108" class="card-body">active session. Pin the shelf to any</text>
    <text x="341" y="128" class="card-body">screen edge and drag converted files</text>
    <text x="341" y="148" class="card-body">directly into your destination app.</text>

    <rect x="638" y="0" width="298" height="170" rx="12" fill="#0E121A" fill-opacity="0.85" stroke="#263042"/>
    <text x="660" y="32" class="kicker">03 · Safety Guarantees</text>
    <text x="660" y="60" class="card-title">Atomic &amp; Collision-Safe</text>
    <text x="660" y="88" class="card-body">Outputs always use collision-safe names.</text>
    <text x="660" y="108" class="card-body">Originals stay untouched unless Move</text>
    <text x="660" y="128" class="card-body">is confirmed. Cancelled/failed batches</text>
    <text x="660" y="148" class="card-body">auto-remove partial batch outputs.</text>
  </g>
"""
write_svg("card-workflow.svg", 1000, 234, workflow_body)

# CARD 03: COVERAGE
coverage_body = """
  <g class="font-sans" transform="translate(32, 30)">
    <rect x="0" y="0" width="220" height="110" rx="12" fill="#0E121A" stroke="#263042"/>
    <text x="24" y="48" class="stat-num">230</text>
    <text x="24" y="74" class="stat-label">Canonical Pairs</text>
    <text x="24" y="94" class="card-body">In active registry</text>

    <rect x="238" y="0" width="220" height="110" rx="12" fill="#0E121A" stroke="#263042"/>
    <text x="262" y="48" class="stat-num">215</text>
    <text x="262" y="74" class="stat-label">Fixture Audit Passed</text>
    <text x="262" y="94" class="card-body">+14 w/ Apple Pages</text>

    <rect x="476" y="0" width="220" height="110" rx="12" fill="#0E121A" stroke="#263042"/>
    <text x="500" y="48" class="stat-num">4 / 6</text>
    <text x="500" y="74" class="stat-label">Tools / File Actions</text>
    <text x="500" y="94" class="card-body">Implemented &amp; verified</text>

    <rect x="714" y="0" width="222" height="110" rx="12" fill="#191512" stroke="#E5D0B1" stroke-opacity="0.45"/>
    <text x="736" y="34" class="kicker">Audit Transparency</text>
    <text x="736" y="60" class="card-body" fill="#F4F6F8">249 pairs / 26 tools</text>
    <text x="736" y="80" class="card-body">target is in progress.</text>
    <text x="736" y="98" class="card-body">GIF→MP3 unsupported.</text>

    <rect x="0" y="128" width="936" height="92" rx="12" fill="#0E121A" stroke="#263042"/>
    <text x="24" y="156" class="kicker">Format Processing Highlights</text>
    <text x="24" y="182" class="card-body">• PDF exports process all pages (300 DPI images) · PDF → DOCX uses OCR text reflow · PDF → XLSX outputs Page/Text columns</text>
    <text x="24" y="204" class="card-body">• PDF → PPTX builds one slide per page · CSV/TSV exports use sheet 1 · GZIP creates .tar.gz · Text clippings save as TXT first</text>
  </g>
"""
write_svg("card-coverage.svg", 1000, 276, coverage_body)

# CARD 04: ENGINES
engines_body = """
  <g class="font-sans" transform="translate(32, 30)">
    <rect x="0" y="0" width="936" height="130" rx="12" fill="#0E121A" stroke="#263042"/>
    <text x="24" y="34" class="kicker">Bundled &amp; System Conversion Engines (Resources/Engines/arm64)</text>
    <text x="24" y="64" class="card-title">FFmpeg 7.1  ·  LibreOffice 26.2.6  ·  Apple Frameworks  ·  System libarchive  ·  Apple Pages (Optional)</text>
    <text x="24" y="92" class="card-body">Scripts/fetch-engines.py downloads ~350 MB from documented upstream sources at build time and verifies SHA-256 checksums</text>
    <text x="24" y="112" class="card-body">before extraction. Pinned upstream URLs, checksums, and licensing notices live in Docs/THIRD_PARTY.md and NOTICE.md.</text>
  </g>
"""
write_svg("card-engines.svg", 1000, 190, engines_body)

# CARD 06: CONTRIBUTING & INSTALLERS
contrib_body = """
  <g class="font-sans" transform="translate(32, 30)">
    <rect x="0" y="0" width="455" height="136" rx="12" fill="#0E121A" stroke="#263042"/>
    <text x="24" y="34" class="kicker">Local DMG Packaging</text>
    <text x="24" y="62" class="card-title">Scripts/build-dmg.sh → output/miracle.dmg</text>
    <text x="24" y="88" class="card-body">Trims unused office assets, ad-hoc signs the app and</text>
    <text x="24" y="108" class="card-body">PDF-import helper, and enforces a strict &lt;270 MB DMG limit.</text>

    <rect x="481" y="0" width="455" height="136" rx="12" fill="#0E121A" stroke="#263042"/>
    <text x="505" y="34" class="kicker">Repository Hygiene &amp; Redistribution</text>
    <text x="505" y="62" class="card-title">Clean Tree &amp; Third-Party Compliance</text>
    <text x="505" y="88" class="card-body">No prebuilt engines, DMGs, or signing keys are committed.</text>
    <text x="505" y="108" class="card-body">Public releases require Apple notarization + source compliance.</text>
  </g>
"""
write_svg("card-contributing.svg", 1000, 196, contrib_body)

# FOOTER SIGNATURE
footer_body = """
  <g class="font-sans" text-anchor="middle">
    <text x="500" y="48" font-size="22" font-weight="800" fill="#F4F6F8" letter-spacing="-0.5">miracle✨</text>
    <text x="500" y="74" font-size="13" fill="#E5D0B1">Offline by design. Built natively for Apple Silicon.</text>
  </g>
"""
write_svg("footer-signature.svg", 1000, 105, footer_body)

print(f"Generated {len(list(OUT_DIR.glob('*.svg')))} SVG assets in {OUT_DIR}/")
