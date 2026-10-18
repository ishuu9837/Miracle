#!/usr/bin/env python3
"""
Generates custom LED Dot-Matrix SVG banners and Bento Cards for Miracle's README.md
matching the obsidian, warm champagne gold, and slate-blue halftone theme.
"""

from pathlib import Path

OUT_DIR = Path("assets/readme")
OUT_DIR.mkdir(parents=True, exist_ok=True)
