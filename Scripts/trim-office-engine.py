#!/usr/bin/env python3
"""Remove desktop extras from the bundled headless office engine.
Removed files are retained outside the source bundle for local recovery.
Filters, fonts, registries, native libraries, and Python remain intact.
"""
from pathlib import Path
import shutil
root=Path(__file__).resolve().parent.parent
resources=root/'Resources/Engines/arm64/LibreOffice.app/Contents/Resources'
backup=root/'.build/office-desktop-extras'
for name in ['help','extensions']:
    source=resources/name
    if source.exists():
        target=backup/name
        target.parent.mkdir(parents=True,exist_ok=True)
        if target.exists(): shutil.rmtree(target)
        shutil.move(str(source),str(target))
for source in (resources/'config').glob('images_*.zip'):
    if source.name in ['images_colibre.zip', 'images_breeze.zip']: continue
    target=backup/'config'/source.name
    target.parent.mkdir(parents=True,exist_ok=True)
    if target.exists(): target.unlink()
    shutil.move(str(source),str(target))
print('Removed help, optional extensions and alternate icons; retained converter resources.')
