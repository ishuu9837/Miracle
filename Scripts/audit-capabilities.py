#!/usr/bin/env python3
"""Enumerate ordered conversion pairs from the native wheel's output registry.

Input spellings below follow the user's requested catalog; extension aliases,
folder packing and same-format re-encoding are not counted as new pairs.
This is a catalog audit, not evidence of successful end-to-end conversions.
"""
import csv
import re
from pathlib import Path

root = Path(__file__).resolve().parent.parent
inputs = {
    'image': 'jpg png webp heic tiff avif svg bmp',
    'audio': 'mp3 m4a wav flac ogg opus aiff wma',
    'video': 'mp4 mov mkv webm gif avi wmv',
    'pdf': 'pdf',
    'document': 'docx pages md txt epub rtf rtfd textclipping',
    'slides': 'ppt pptx',
    'spreadsheet': 'xlsx ods csv tsv xls',
    'subtitle': 'srt vtt',
    'archive': 'zip tar gz rar',
}
registry = (root / 'Core/Capability/CapabilityRegistry.swift').read_text()
outputs = {category: re.findall(r'"([a-z0-9]+)"', values)
           for category, values in re.findall(r'case \.(\w+): formats = \[([^\]]*)\]', registry)}
outputs['document'] = ['docx', 'pages', 'md', 'txt', 'epub', 'html']
path = root / 'Docs/CONVERSION_CATALOG.csv'
counts = {}
with path.open('w', newline='') as file:
    writer = csv.writer(file)
    writer.writerow(['category', 'input', 'output', 'implementation', 'verification', 'conditions'])
    for category, extensions in inputs.items():
        counts[category] = 0
        for source in extensions.split():
            for target in outputs[category] + (['pdf'] if category == 'document' and source in ('docx', 'pages') else []):
                if source == target:
                    continue
                counts[category] += 1
                notes = []
                if source == 'gif' and target == 'mp3': notes.append('Unavailable: GIF has no audio track')
                if target == 'pages': notes.append('Requires installed Apple Pages and Automation permission')
                if category == 'pdf' and target in ('docx', 'pages', 'txt', 'md', 'epub'): notes.append('Text reflow; OCR when page has no text')
                if category == 'pdf' and target == 'xlsx': notes.append('Page/Text columns; no table reconstruction')
                if category in ('pdf', 'slides') and target in ('jpg', 'png'): notes.append('All pages at 300 DPI')
                if category == 'document': notes.append('Text-first; complex layout/media may change')
                if source == 'textclipping': notes.append('Supported UTI-Data plist clippings; legacy resource-fork clippings may require TXT export')
                if target == 'gz': notes.append('Produces .tar.gz')
                writer.writerow([category, source, target, 'Registered', 'Not individually verified', '; '.join(notes)])
print('Requested canonical input/output pairs (excluding self-conversions):')
for category, count in counts.items(): print(f'{category}: {count}')
print(f'Total: {sum(counts.values())}')
print(f'Catalog: {path}')
