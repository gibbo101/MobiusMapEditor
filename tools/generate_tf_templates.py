#!/usr/bin/env python3
"""Generate RedAlert/TemplateTypesTF.cs from the Tiberian Factions tile map.

Reads:
  - the mod's scripts/td_ra_tile_map.json (td_name -> theatre -> {ra_name, ra_id, size})
  - the editor's TiberianDawn/TemplateTypes.cs (authoritative TD land-type/mask strings)

Emits one TemplateType declaration per unique engine template (ra_id), into a
partial-class extension of MobiusEditor.RedAlert.TemplateTypes. Theatre
availability is not declared: the editor resolves it at runtime from the
remaster tileset XML and classic iconsets.

Usage: generate_tf_templates.py [--map <td_ra_tile_map.json>] [--out <TemplateTypesTF.cs>]
"""
import argparse
import json
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
FORK = os.path.dirname(HERE)
DEFAULT_MAP = os.path.expanduser(
    '~/Documents/development/cnc-remastered-mods/cnc-ra-tiberian-factions/scripts/td_ra_tile_map.json')
TD_TEMPLATES = os.path.join(FORK, 'CnCTDRAMapEditor', 'TiberianDawn', 'TemplateTypes.cs')
DEFAULT_OUT = os.path.join(FORK, 'CnCTDRAMapEditor', 'RedAlert', 'TemplateTypesTF.cs')

HEADER = '''//
// Tiberian Factions: TD terrain templates as RA engine types (ids 401+).
// GENERATED FILE - do not edit by hand. Regenerate with tools/generate_tf_templates.py
// from the mod's scripts/td_ra_tile_map.json and the TD template data.
//
// Land-type and usage-mask strings are taken from the Tiberian Dawn template
// declarations; theatre availability is resolved at runtime from the tileset
// XML, so no theatre data is declared here.
using MobiusEditor.Model;

namespace MobiusEditor.RedAlert
{
    public static partial class TemplateTypes
    {
'''

FOOTER = '''    }
}
'''


def split_args(argstr):
    """Split C# argument list on top-level commas (respects quotes and braces)."""
    args, depth, cur, instr, inchr = [], 0, '', False, False
    i = 0
    while i < len(argstr):
        c = argstr[i]
        if instr:
            cur += c
            if c == '\\':
                cur += argstr[i + 1]
                i += 1
            elif c == '"':
                instr = False
        elif inchr:
            cur += c
            if c == "'":
                inchr = False
        elif c == '"':
            instr = True
            cur += c
        elif c == "'":
            inchr = True
            cur += c
        elif c in '([{':
            depth += 1
            cur += c
        elif c in ')]}':
            depth -= 1
            cur += c
        elif c == ',' and depth == 0:
            args.append(cur.strip())
            cur = ''
        else:
            cur += c
        i += 1
    if cur.strip():
        args.append(cur.strip())
    return args


def parse_td_declarations(path):
    """Return {td_name: (w, h, lands, mask)} from TiberianDawn/TemplateTypes.cs."""
    src = open(path, encoding='utf-8-sig').read()
    decls = {}
    for m in re.finditer(r'new TemplateType\((.*?)\);', src, re.DOTALL):
        args = split_args(m.group(1))
        if len(args) < 2:
            continue
        try:
            _ = int(args[0])
        except ValueError:
            continue
        name_m = re.match(r'^"(.+)"$', args[1])
        if not name_m:
            continue
        name = name_m.group(1)
        w = h = None
        lands = mask = None
        rest = args[2:]
        if len(rest) >= 2 and rest[0].isdigit() and rest[1].isdigit():
            w, h = int(rest[0]), int(rest[1])
            rest = rest[2:]
        strings = [re.match(r'^"(.*)"$', a).group(1) for a in rest if re.match(r'^".*"$', a)]
        chars = [a for a in rest if re.match(r"^'.'$", a)]
        if strings:
            lands = strings[0]
            for s in strings[1:]:
                if s and all(ch in '01 ' for ch in s):
                    mask = s
                    break
        elif chars:
            lands = chars[0][1]
        if w is None:
            w = h = 1
        decls[name.lower()] = (w, h, lands, mask)
    return decls


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--map', default=DEFAULT_MAP)
    ap.add_argument('--out', default=DEFAULT_OUT)
    args = ap.parse_args()

    tile_map = json.load(open(args.map))
    td_decls = parse_td_declarations(TD_TEMPLATES)

    entries = {}  # ra_id -> (ra_name, w, h, lands, mask, td_name, theatres)
    for td_name, theatres in tile_map.items():
        for theatre, info in theatres.items():
            ra_id, ra_name = info['ra_id'], info['ra_name']
            w, h = info['size']
            if ra_id in entries:
                entries[ra_id][6].append(theatre)
                continue
            if td_name.lower() == 'clear1':
                # TD clear ground: 16 positional icons (4x4 pattern), stored per-icon in maps.
                entries[ra_id] = [ra_name, 4, 4, ' '.join(['CCCC'] * 4), None, td_name, [theatre]]
                continue
            if td_name.lower() not in td_decls:
                print(f'WARNING: no TD declaration for {td_name}; using clear land types',
                      file=sys.stderr)
                lands, mask = None, None
                tw, th = w, h
            else:
                tw, th, lands, mask = td_decls[td_name.lower()]
            if (tw, th) != (w, h):
                print(f'WARNING: size mismatch for {td_name} ({ra_name}): '
                      f'TD {tw}x{th} vs map {w}x{h}; using map size', file=sys.stderr)
                lands, mask = None, None
            if lands is None:
                lands = ' '.join('C' * w for _ in range(h))
            entries[ra_id] = [ra_name, w, h, lands, mask, td_name, [theatre]]

    lines = [HEADER]
    for ra_id in sorted(entries):
        ra_name, w, h, lands, mask, td_name, theatres = entries[ra_id]
        field = ra_name.capitalize()
        name_lc = ra_name.lower()
        comment = f'TD {td_name} ({"/".join(sorted(theatres))})'
        if mask:
            decl = (f'        public static readonly TemplateType {field} = '
                    f'new TemplateType({ra_id}, "{name_lc}", {w}, {h}, "{lands}", "{mask}"); // {comment}')
        else:
            decl = (f'        public static readonly TemplateType {field} = '
                    f'new TemplateType({ra_id}, "{name_lc}", {w}, {h}, "{lands}"); // {comment}')
        lines.append(decl + '\n')
    lines.append(FOOTER)
    with open(args.out, 'w') as f:
        f.writelines(lines)
    print(f'wrote {len(entries)} declarations to {args.out}')


if __name__ == '__main__':
    main()
