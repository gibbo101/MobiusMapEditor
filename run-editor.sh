#!/usr/bin/env bash
# Launch the Tiberian Factions Mobius Map Editor natively on Linux (mono).
# Run from anywhere; starts in the game dir so CONFIG.MEG autodetection passes.
set -e
EXE="$(dirname "$(readlink -f "$0")")/CnCTDRAMapEditor/bin/Release/MobiusMapEditor.exe"
cd "$HOME/.steam/steam/steamapps/common/CnCRemastered"
exec mono "$EXE" "$@"
