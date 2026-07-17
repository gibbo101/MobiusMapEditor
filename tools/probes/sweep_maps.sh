#!/bin/bash
# Regression sweep: load->save->codec-diff every shipped CustomMaps .MPR through the
# editor plugin (RoundTripProbe) and diff the packed sections against the original.
# Never touches the originals; saves go to /tmp/sweep-out/.
set -u
HERE="$(cd "$(dirname "$0")" && pwd)"
REL="$HERE/../../CnCTDRAMapEditor/bin/Release"
MOD=~/Documents/development/cnc-remastered-mods/cnc-ra-tiberian-factions
MAPS="$MOD/build/remaster/Vanilla_RA/CustomMaps"
OUT=/tmp/sweep-out
mkdir -p "$OUT"

mono-csc -r:"$REL/MobiusMapEditor.exe" -r:System.Drawing.dll -out:/tmp/rtprobe.exe "$HERE/RoundTripProbe.cs" || exit 1

pass=0; fail=0
for map in "$MAPS"/*.MPR "$MAPS"/*.mpr; do
    [ -e "$map" ] || continue
    name=$(basename "$map")
    saved="$OUT/$name"
    log="$OUT/$name.log"
    (cd "$REL" && MONO_PATH="$REL:$REL/bin" timeout 300 mono /tmp/rtprobe.exe "$map" "$saved") > "$log" 2>&1
    if [ ! -f "$saved" ]; then
        echo "FAIL  $name  (probe crashed; see $log)"
        fail=$((fail+1))
        continue
    fi
    warnings=$(grep -c "^load errors: [^0]" "$log" || true)
    if python3 "$HERE/codec_diff.py" "$map" "$saved" >> "$log" 2>&1; then
        if [ "$warnings" -eq 0 ]; then
            echo "PASS  $name"
        else
            echo "PASS* $name  (load warnings; see $log)"
        fi
        pass=$((pass+1))
    else
        echo "FAIL  $name  (codec diff; see $log)"
        fail=$((fail+1))
    fi
done
echo "----"
echo "pass=$pass fail=$fail"
[ "$fail" -eq 0 ]
