#!/usr/bin/env python3
"""Diff the packed cell sections of two RA .MPR maps using the mod's codec.

Usage: codec_diff.py <original.mpr> <saved.mpr>
Prints per-section diff counts for [TFTDTiles], [MapPack], [OverlayPack];
exit code 0 when all present sections match, 1 otherwise.
"""
import sys
import os

MOD_SCRIPTS = os.path.expanduser(
    '~/Documents/development/cnc-remastered-mods/cnc-ra-tiberian-factions/scripts')
sys.path.insert(0, MOD_SCRIPTS)
import td_map_to_ra as codec  # noqa: E402

SECTIONS = ('TFTDTiles', 'MapPack', 'OverlayPack')


def cells(path):
    secs = codec.read_ini(path)
    return {n: (codec.section_pack(secs, n) if n in secs else None) for n in SECTIONS}


def normalize_mappack(data):
    """Explicit clear (template 0, icon 0) and 'no template' (0xFFFF) are the same
    thing to the game; the editor saves the latter. Canonicalise before diffing."""
    b = bytearray(data)
    for c in range(16384):
        if b[c * 2] == 0 and b[c * 2 + 1] == 0 and b[32768 + c] == 0:
            b[c * 2] = b[c * 2 + 1] = 0xFF
    return bytes(b)


def main():
    orig, new = cells(sys.argv[1]), cells(sys.argv[2])
    ok = True
    for n in SECTIONS:
        a, b = orig[n], new[n]
        if a is None and b is None:
            print(f'{n}: absent in both')
            continue
        if a is None or b is None:
            print(f'{n}: MISSING (orig={a is not None}, saved={b is not None})')
            ok = False
            continue
        if len(a) != len(b):
            print(f'{n}: LENGTH {len(a)} vs {len(b)}')
            ok = False
            continue
        if n == 'MapPack' and len(a) == 49152:
            a, b = normalize_mappack(a), normalize_mappack(b)
        diffs = sum(1 for x, y in zip(a, b) if x != y)
        print(f'{n}: {diffs} diffs / {len(a)} bytes')
        ok &= diffs == 0
    sys.exit(0 if ok else 1)


if __name__ == '__main__':
    main()
