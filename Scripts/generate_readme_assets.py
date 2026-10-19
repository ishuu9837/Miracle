#!/usr/bin/env python3
"""Generate Miracle's self-contained, XML-valid README graphics (stdlib only).

Run from any directory. Edit the release copy here and in README.md together when
publishing a new release. Graphics supplement readable Markdown, not replace it.
"""
from html import escape
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "assets" / "readme"
OUT.mkdir(parents=True, exist_ok=True)
DEFS = '''<defs>
<linearGradient id="bg" x2="1" y2="1"><stop stop-color="#07080A"/><stop offset=".5" stop-color="#121824"/><stop offset="1" stop-color="#080A0F"/></linearGradient>
<radialGradient id="glow"><stop stop-color="#E5D0B1" stop-opacity=".16"/><stop offset="1" stop-color="#E5D0B1" stop-opacity="0"/></radialGradient>
<pattern id="dots" width="10" height="10" patternUnits="userSpaceOnUse"><circle cx="5" cy="5" r="1.5" fill="#A6BBD2" opacity=".13"/></pattern>
<style>text{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif} .mono{font-family:Menlo,Consolas,monospace;letter-spacing:2px} .gold{fill:#E5D0B1}.white{fill:#F4F6F8}.muted{fill:#B5C1D1}</style></defs>'''

def text(x, y, value, size=19, cls="muted", weight=400):
    return f'<text x="{x}" y="{y}" font-size="{size}" font-weight="{weight}" class="{cls}">{escape(value)}</text>'

def write(name, height, label, content):
    svg = f'''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1000 {height}" width="1000" height="{height}" role="img" aria-labelledby="title">
<title id="title">{escape(label)}</title>{DEFS}
<rect x="1" y="1" width="998" height="{height-2}" rx="22" fill="url(#bg)" stroke="#6E7F96" stroke-opacity=".4"/>
<rect x="2" y="2" width="996" height="{height-4}" rx="22" fill="url(#dots)"/>
<ellipse cx="850" cy="80" rx="300" ry="220" fill="url(#glow)"/>
{content}</svg>'''
    (OUT / name).write_text(svg + "\n", encoding="utf-8")

def card(name, kicker, title, lines):
    height = 146 + len(lines) * 31
    body = text(38, 40, kicker, 11, "mono gold", 700) + text(38, 83, title, 30, "white", 700)
    body += '<path d="M38 104H962" stroke="#E5D0B1" stroke-opacity=".25"/>'
    body += ''.join(text(38, 140 + i * 31, line) for i, line in enumerate(lines))
    write(name, height, title + ". " + " ".join(lines), body)

hero = text(42, 45, "MAC + WINDOWS / LOCAL FILE CONVERSION", 12, "mono gold", 700)
hero += text(38, 151, "miracle", 112, "white", 700)
hero += text(435, 118, "✦", 57, "gold")
hero += text(42, 206, "Your files. A different form.", 30, "gold", 500)
hero += text(42, 247, "Apple Silicon · Windows x64 / ARM64 · Open source", 20)
hero += '<rect x="42" y="277" width="635" height="39" rx="8" fill="#E5D0B1"/>'
hero += text(58, 303, "MAC 0.3.0 TEST  /  WINDOWS 0.3.2 PREVIEW", 15, "mono", 700)
write("hero-banner.svg", 350, "Miracle — local file conversion for Mac and Windows", hero)

sections = [
    ("overview", "01 / OVERVIEW", "Local power. Two native desktops."),
    ("workflow", "02 / INTERACTION", "Choose a file. Find its next form."),
    ("coverage", "03 / COVERAGE", "Evidence before big numbers."),
    ("engines", "04 / ARCHITECTURE", "Local engines. Clear boundaries."),
    ("build", "05 / BUILD", "Xcode on Mac. .NET on Windows."),
    ("contributing", "06 / CONTRIBUTING", "Build it. Check it. Make it better."),
]
for slug, kicker, title in sections:
    write(f"title-{slug}.svg", 108, title,
          text(30, 34, kicker, 11, "mono gold", 700) + text(30, 78, title, 29, "white", 600))
card("card-overview.svg", "YOUR COMPUTER. YOUR FILES.", "A little file magic, on your desktop.", [
    "Mac: Swift / SwiftUI / AppKit, built for Apple Silicon.",
    "Windows: C# / WPF / .NET 10, with x64 and ARM64 downloads.",
    "Bundled engines run conversions locally. No account or file uploads.",
    "Preview builds have documented limits; support differs by platform."])
card("card-workflow.svg", "PICK / SHIFT / SLIDE", "Buttons when you want them. A wheel when you need it.", [
    "Mac: select a supported file in Finder, tap Shift, choose a format.",
    "Windows: Add files → colorful output button → Convert file.",
    "Or drag from Explorer: Shift opens the wheel; later taps cycle sets.",
    "Slide and release on an option. Tools open settings; Esc cancels."])
card("card-coverage.svg", "WINDOWS 0.3.2 / RELEASE EVIDENCE", "303 feature checks. 47 regressions per architecture.", [
    "x64: 303/303 feature checks + 47/47 regressions under ARM emulation.",
    "ARM64: 47/47 regressions; no full feature rerun for this UI update.",
    "Local Defender scans: no threats in both extracted app/engine folders.",
    "Unsigned preview. Manual Explorer drag and native x64 checks pending."])
card("card-engines.svg", "BUILT TO WORK LOCALLY", "Native apps. Bundled conversion engines.", [
    "Mac uses Apple frameworks, FFmpeg, LibreOffice and system libarchive.",
    "Windows bundles media, image, PDF, document and archive engines.",
    "Windows Pages export and OCR are unavailable; fidelity limits apply.",
    "App source: MIT. Third-party engines retain their own licenses."])
card("card-contributing.svg", "SOURCE / ARTIFACTS / VERIFICATION", "Reproducible steps, honest release notes.", [
    "Mac test DMG: 246.9 MB. Apple Silicon only. Not notarized.",
    "Windows 0.3.2: x64 275.7 MB / ARM64 240.9 MB downloads.",
    "Windows packages include offline engines and expand to about 1.3 GB.",
    "Compare checksums and report reproducible, nonsensitive examples."])
write("footer-signature.svg", 126, "Miracle — a little file magic for Mac and Windows",
      text(36, 55, "miracle ✦", 34, "white", 700) + text(36, 94, "LOCAL FILES. OPEN SOURCE. TWO NATIVE DESKTOPS.", 12, "mono gold", 700))
print(f"Generated {len(list(OUT.glob('*.svg')))} README SVG assets in {OUT}")
