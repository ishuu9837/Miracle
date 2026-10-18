#!/usr/bin/env python3
"""Fetch the pinned Apple Silicon converters for a local source build.

Downloaded executables are never tracked by Git. SHA-256 is verified before
extraction. Run --check to validate the expected local paths without downloading.
"""
import argparse
import hashlib
import shutil
import subprocess
import tempfile
import urllib.request
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
ENGINE = ROOT / 'Resources/Engines/arm64'
WHEEL_URL = 'https://files.pythonhosted.org/packages/40/5c/f3d8a657d362cc93b81aab8feda487317da5b5d31c0e1fdfd5e986e55d17/imageio_ffmpeg-0.6.0-py3-none-macosx_11_0_arm64.whl'
WHEEL_SHA = 'b1ae3173414b5fc5f538a726c4e48ea97edc0d2cdc11f103afee655c463fa742'
OFFICE_URL = 'https://download.documentfoundation.org/libreoffice/stable/26.2.6/mac/aarch64/LibreOffice_26.2.6_MacOS_aarch64.dmg'
OFFICE_SHA = '94bb3248df074c225490a8a6d1d9dc87c7d6783dbb7a8e9f0d0c3d94348552af'


def digest(path):
    result = hashlib.sha256()
    with path.open('rb') as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b''):
            result.update(chunk)
    return result.hexdigest()


def download(url, destination, expected):
    if destination.exists() and digest(destination) == expected:
        return destination
    partial = destination.with_suffix(destination.suffix + '.part')
    try:
        print(f'Downloading {destination.name}…', flush=True)
        request = urllib.request.Request(url, headers={'User-Agent': 'Miracle-source-setup/0.3.0'})
        with urllib.request.urlopen(request, timeout=90) as response, partial.open('wb') as output:
            shutil.copyfileobj(response, output)
        if digest(partial) != expected:
            raise RuntimeError(f'Checksum mismatch for {destination.name}; no files were extracted.')
        partial.replace(destination)
    finally:
        partial.unlink(missing_ok=True)
    return destination


def install_ffmpeg(cache):
    destination = ENGINE / 'ffmpeg'
    if destination.is_file():
        print('FFmpeg already present; keeping the local copy.')
        return
    wheel = download(WHEEL_URL, cache / 'imageio-ffmpeg-0.6.0-arm64.whl', WHEEL_SHA)
    with zipfile.ZipFile(wheel) as archive:
        candidates = [name for name in archive.namelist()
                      if '/binaries/ffmpeg-' in name and not name.endswith('/')]
        if len(candidates) != 1:
            raise RuntimeError('The pinned wheel does not contain exactly one FFmpeg executable.')
        with archive.open(candidates[0]) as source, destination.open('wb') as output:
            shutil.copyfileobj(source, output)
    destination.chmod(0o755)


def install_office(cache):
    destination = ENGINE / 'LibreOffice.app'
    if destination.exists():
        print('LibreOffice already present; keeping the local copy.')
        return
    image = download(OFFICE_URL, cache / 'LibreOffice-26.2.6-arm64.dmg', OFFICE_SHA)
    with tempfile.TemporaryDirectory(prefix='miracle-office-', dir=cache) as work:
        mount = Path(work) / 'mounted'
        subprocess.run(['hdiutil', 'attach', str(image), '-readonly', '-nobrowse',
                        '-mountpoint', str(mount)], check=True)
        try:
            app = mount / 'LibreOffice.app'
            if not (app / 'Contents/MacOS/soffice').is_file():
                raise RuntimeError('The pinned installer is missing the office executable.')
            subprocess.run(['ditto', str(app), str(destination)], check=True)
        finally:
            subprocess.run(['hdiutil', 'detach', str(mount)], check=True)


def check():
    paths = [ENGINE / 'ffmpeg', ENGINE / 'LibreOffice.app/Contents/MacOS/soffice']
    missing = [str(path.relative_to(ROOT)) for path in paths if not path.is_file()]
    if missing:
        raise RuntimeError('Missing engines: ' + ', '.join(missing))
    print('Apple Silicon FFmpeg and LibreOffice paths are ready.')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true', help='Check local paths without downloading')
    arguments = parser.parse_args()
    if arguments.check:
        check()
        return
    if subprocess.check_output(['uname', '-m'], text=True).strip() != 'arm64':
        raise RuntimeError('Miracle source builds target Apple Silicon Macs only.')
    cache = ROOT / '.build/downloads'
    cache.mkdir(parents=True, exist_ok=True)
    ENGINE.mkdir(parents=True, exist_ok=True)
    install_ffmpeg(cache)
    install_office(cache)
    check()
    print('Run Scripts/build-dmg.sh to trim, sign and package the engines with Miracle.')


if __name__ == '__main__':
    main()
