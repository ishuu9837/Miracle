#!/usr/bin/env python3
"""Generate Miracle's self-contained, XML-valid README graphics (stdlib only).

Run from any directory. Edit the release copy here and in README.md together when
publishing a new release. Graphics supplement readable Markdown, not replace it.
"""
from html import escape
from pathlib import Path
import xml.etree.ElementTree as ET

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

def centered(x, y, value, size, cls="muted", weight=400, spacing=0):
    return f'<text x="{x}" y="{y}" text-anchor="middle" font-size="{size}" font-weight="{weight}" letter-spacing="{spacing}" class="{cls}">{escape(value)}</text>'

def sparkle(x, y, radius, color):
    return f'<path d="M{x} {y-radius} Q{x+radius*.16} {y-radius*.16} {x+radius} {y} Q{x+radius*.16} {y+radius*.16} {x} {y+radius} Q{x-radius*.16} {y+radius*.16} {x-radius} {y} Q{x-radius*.16} {y-radius*.16} {x} {y-radius}Z" fill="{color}"/>'

hero = '<ellipse cx="250" cy="120" rx="370" ry="185" fill="url(#glow)"/>'
hero += centered(485, 140, "miracle", 82, "white", 700, -4)
hero += sparkle(652, 99, 23, "#F4F6F8") + sparkle(684, 73, 9, "#E5D0B1")
hero += centered(500, 194, "Open-source, offline file conversion for Mac & Windows", 18, "gold", 500, .6)
hero += centered(500, 226, "Native Swift + WPF/.NET · Apple Silicon & Windows x64 / ARM64 · Local engines", 14, "muted", 400, .3)
write("hero-banner.svg", 280, "Miracle — open-source, offline file conversion for Mac and Windows", hero)

# One local SVG keeps the centered badge layout consistent on GitHub without
# relying on third-party badge services or external fonts.
def icon(name, x, y, size=24):
    # Vendored vectors are embedded, so GitHub needs no nested image requests.
    source = ET.parse(OUT / "icons" / f"{name}.svg").getroot()
    paths = ''.join(f'<path d="{escape(node.attrib["d"], quote=True)}" fill="#E5D0B1"/>'
                    for node in source.iter('{http://www.w3.org/2000/svg}path'))
    return f'<g transform="translate({x} {y}) scale({size / 24})">{paths}</g>'

def badge(x, y, width, split, label, value, font=20, fill="#1C2331", icons=()):
    body = (f'<rect x="{x}" y="{y}" width="{split}" height="52" fill="#07090C"/>'
            f'<rect x="{x+split}" y="{y}" width="{width-split}" height="52" fill="{fill}"/>')
    for index, name in enumerate(icons):
        body += icon(name, x + (12 if len(icons) == 2 else 18) + index * 30, y + 14)
    offset = 30 if len(icons) == 2 else 14 if icons else 0
    body += centered(x+split/2+offset, y+33, label, 16 if len(icons) == 2 else 18,
                     "white", 400, 2 if len(icons) == 2 else 2.8)
    body += centered(x+split+(width-split)/2, y+33, value, font, "white", 700, 2.1)
    return body
badges = badge(145, 12, 710, 190, "PLATFORM", "MACOS 13+ / WINDOWS 11", icons=("apple", "windows"))
badges += badge(45, 80, 910, 160, "ARCH", "APPLE SILICON · WINDOWS X64 / ARM64", 19, icons=("apple",))
badges += badge(91, 148, 470, 140, "STACK", "SWIFT 6 / .NET 10", 18, icons=("swift",))
badges += badge(579, 148, 330, 140, "MODE", "LOCAL", 20)
badges += badge(340, 216, 320, 180, "LICENSE", "MIT", 22, "#3A3228")
(OUT / "platform-badges.svg").write_text(
    '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1000 284" width="1000" height="284" role="img" aria-labelledby="title"><title id="title">macOS 13+ and Windows 11; Apple Silicon, x64 and ARM64; Swift 6 and .NET 10; local conversion; MIT app source</title>'
    + DEFS + "<!-- Apple and Swift: Simple Icons, CC0; see icons/LICENSE.txt and icons/NOTICE.txt. -->" + badges + '</svg>\n', encoding="utf-8")

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
