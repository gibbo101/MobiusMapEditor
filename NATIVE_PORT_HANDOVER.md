# Mobius Map Editor — native Linux port + mod-aware tiles (handover)

Written 2026-07-16 (late night) for a fresh instance picking this up. Luke is asleep;
he kicked off this exploration to see how hard a **native Linux** version of the editor is,
and to make it understand **our mod's custom TD tiles** so he can author maps (esp. desert)
without the TD-author-then-transcode dance.

## Why this exists (context from tonight's session)

Working in the mod (`cnc-ra-tiberian-factions`) we hit two tileset bugs on TD maps:
1. **Temperate coastal shores rendered as white squares** — FIXED + shipped (commit `ede7ca1`
   in the mod repo). Root cause: `scripts/build_td_tiles.py` spliced the shared `TF_TD_TILES`
   XML marker once *per theatre letter*, and `T`(temperate)+`S`(winter) both target
   `RA_TERRAIN_TEMPERATE.XML`, so the winter pass overwrote the temperate shore/bridge block.
2. **"Tiberium Gardens" pond shore shows water boxes** — diagnosed, NOT fixed. When Tiberium
   overlay sits on a TD shore cell, the TD art is suppressed (anti-z-fight rule in
   `dllinterface.cpp Cell_Class_Draw_It`) and `cell.cpp Get_Template_Info` falls back to an RA
   shore/water tile (`SH02`/`WATER`) for that cell → blue "puddle". A code fix (grass fallback)
   was tried and **reverted** — Luke will tidy affected maps by hand.

Bug #2 is exactly the kind of thing a **mod-aware editor** would let him fix directly: today the
editor can't even *see* our tiles, so it can't author or repair the TD-tile layer of our maps.

## The goal, in two independent tracks

These are separable — do NOT couple them:

### Track A — mod-aware tiles (medium effort, high value, runs under Proton today)
Make a fork of the editor understand our custom TD tiles per theatre, so Luke can place them and
save maps that round-trip our authentic-tile layer.
- The editor already uses the **same named-tileset architecture we mod**:
  `RedAlert/TheaterTypes.cs` → `Temperate="RA_Terrain_Temperate"`, `Snow="RA_Terrain_Snow"`,
  `Interior="RA_Terrain_Interior"` — the exact names of our `RA_TERRAIN_*.XML` tileset files.
- Templates are one-line declarations in `RedAlert/TemplateTypes.cs`:
  `new TemplateType(4, "sh02", 5, 5, {Temperate, Snow})`. We add ours, e.g.
  `new TemplateType(401, "tdsh1", 3, 3, {Temperate})`. These can be **auto-generated** from the
  mod's `scripts/td_ra_tile_map.json` (`{td_name: {ra_name, ra_id, size}}`) — same data our DLL
  codegen already uses.
- Art loads via `Utility/TilesetManager.cs` + `Utility/Tileset.cs` + `Utility/TextureManager.cs`.
  **OPEN SPIKE (do this first):** confirm whether TilesetManager can load our **loose ZIP/TGA**
  art (mod `Data/ART/TEXTURES/.../TERRAIN/<THEATRE>/TD<NAME>.ZIP` + the `<Tile>` blocks in our
  tileset XML) or whether it only reads MEG-packed textures. TilesetManager already resolves
  files relative to the tileset XML path (`Path.Combine(GetDirectoryName(xmlPath), ...)`), which
  is promising. If loose art won't load, alternative = feed it our **classic** iconsets (`.tem`
  staged by the mod's `scripts/build_tfassets.sh` / `scripts/_td_tems`).
- **`[TFTDTiles]` round-trip:** our maps store the authentic TD-tile layer in a custom
  `[TFTDTiles]` section (LCW-block + UUBlock, `16384×u16 TType ++ 16384×u8 TIcon`), with
  `[MapPack]` holding only the vanilla RA fallback. The editor must read/write `[TFTDTiles]` or
  it will desync the two layers on save. Codec reference: the mod's
  `scripts/td_map_to_ra.py` (`lcw_block_decompress/compress`, `uublock_decode/encode`).

### Track B — native Linux (large effort, the thing Luke wants explored tonight)
The editor is **WinForms, .NET Framework 4.7.2** — Windows-only GUI. Native Linux ⇒ retarget +
replace the UI toolkit. Coupling reality (measured tonight): **278 `.cs` files, 176 touch
`System.Windows.Forms`/`System.Drawing`.**
- **Portable core** (moves to net8/net10 with minor changes): `Model/`, `Utility/`, `RedAlert/`,
  `TiberianDawn/`, `Render/` logic, file-format I/O.
- **Windows-locked, must rewrite:** ~90 files of WinForms UI — `Dialogs/` (28), `Controls/` (27),
  `Tools/Dialogs/` (23), `Tools/` (12).
- **Rendering:** heavy `System.Drawing` (GDI+). On Linux either lean on
  `System.Drawing.Common`+libgdiplus or move to **SkiaSharp**.
- **Recommended target: Avalonia + .NET 8/10** (cross-platform native XAML desktop). WinForms
  never runs natively on Linux even on modern .NET; Avalonia is the real path.
- Realistic effort: **weeks to months** for a feature-rich editor. Suggest an incremental
  strangler approach — stand up an Avalonia shell, move the map-render surface first, port
  dialogs/tools one at a time, keep the portable core shared.

**Smart sequencing:** do Track A first (mechanical, runs under Proton, unblocks Luke's maps now),
which forces you to understand the tileset + file-format code — THEN Track B port is far safer
because you already know the codebase.

## Environment already set up tonight (everything downloaded)

- **Working fork:** `~/Documents/development/cnc-remastered-mods/mobius-editor/`
  - `origin` = `git@github.com:gibbo101/MobiusMapEditor.git` (Luke's GitHub fork — HIS project)
  - `upstream` = `https://github.com/Nyerguds/MobiusMapEditor.git` (for pulling their bugfixes;
    **we will NOT PR upstream — this fork is mod-exclusive and will diverge hard**)
  - HEAD `b49ebe8`, targets `net4.7.2`.
- **Prebuilt editor (for running under Proton + reference):**
  `mobius-editor/_prebuilt/MobiusMapEditor_1.6.1.0/MobiusMapEditor.exe` (git-excluded).
- **.NET SDKs (installed to `~/.dotnet`, no sudo):** `8.0.421` AND `10.0.302`.
  Add to PATH: `export PATH="$HOME/.dotnet:$PATH"`.
- **Avalonia templates installed + build-verified on this Linux box** (net10 `avalonia.app`
  scaffolds and builds clean, 0 errors). `dotnet new avalonia.app|avalonia.mvvm|avalonia.xplat`.
- **NOT installed (needs `sudo apt`, ask Luke):** `mono`/`msbuild` — only needed if you want to
  build the EXISTING net4.7.2 WinForms editor on Linux. The native (Avalonia/net8+) path does
  NOT need Mono.

## Pristine reference (do NOT modify)

- `~/Documents/development/cnc-remastered-mods/reference/MobiusMapEditor/` — read-only upstream
  clone (older, ~May 2025). Keep pristine for diffing; work in `mobius-editor/` instead.
- EA original editor source also ships in the game install:
  `~/.steam/steam/steamapps/common/CnCRemastered/SOURCECODE/CnCTDRAMapEditor/` and the shipped
  binary `~/.steam/steam/steamapps/common/CnCRemastered/CnCTDRAMapEditor.exe`.

## Mod-side files the editor work will touch (in `cnc-ra-tiberian-factions/`)

- `scripts/td_ra_tile_map.json` — our tile map (source for auto-generating TemplateType decls).
- `scripts/build_td_tiles.py` — how our tiles + tileset XML + DLL codegen are produced.
- `scripts/td_map_to_ra.py` — the transcoder + `[MapPack]`/`[OverlayPack]`/`[TFTDTiles]` codec.
- `resources/remaster_mods/Vanilla_RA/Data/XML/TILESETS/RA_TERRAIN_{TEMPERATE,INTERIOR}.XML`
  — our tileset XML (temperate = `TD*` + `TDW*`; interior = desert `TD*`).
- `resources/remaster_mods/Vanilla_RA/Data/ART/TEXTURES/SRGB/RED_ALERT/TERRAIN/<THEATRE>/TD*.ZIP`
  — our loose HD tile art.
- `redalert/cdata.cpp` / `redalert/defines.h` — the DLL-side template enum + registrations
  (`TEMPLATE_TDSH1`+, ids 401+) that define the tiles the editor must mirror.

## Licensing

GPL v3 (EA released the editor + DLLs under GPL v3, plus EA's additional terms). The fork stays
GPL v3; keep upstream attribution. Fine to diverge and self-host; the mod itself is already GPL v3.

## Suggested first moves for the overnight instance

1. `export PATH="$HOME/.dotnet:$PATH"`; open the fork; skim `Utility/TilesetManager.cs`,
   `Utility/Tileset.cs`, `RedAlert/TheaterTypes.cs`, `RedAlert/TemplateTypes.cs`.
2. **Track A spike:** determine the exact art-loading path (MEG vs loose) — the single biggest
   unknown. Write it up.
3. **Track B spike:** stand up a throwaway Avalonia shell, try referencing the fork's `Model/` +
   `Utility/` from a net8 project to gauge how cleanly the core detaches from WinForms/GDI+.
   Expect `System.Drawing` friction — note every hard dependency.
4. Write findings back into this file (append a dated section). Leave Luke a crisp
   go/no-go + effort estimate for the native port.

---

# OVERNIGHT FINDINGS — 2026-07-16 (all major spikes RESOLVED, Track A largely IMPLEMENTED)

Everything below was done and verified on this machine overnight. The editor now **builds on
Linux** (`dotnet build` + mono reference assemblies), **runs under mono** (real WinForms window,
verified via screenshot until the desktop session locked), and **loads the mod's TD tile art
headlessly** (verified end-to-end by probe programs, no GUI needed).

## Result summary

| Question | Answer |
|---|---|
| Can the editor load our loose ZIP/TGA art? | **YES — with zero art-pipeline changes.** `TextureManager` tries `<frame-dir>.ZIP` first and the mod-path overlay in `MegafileManager.OpenFile` resolves our `Data/ART/.../TDSH10.ZIP` files. Verified: `tdsh10`/`tdsh1` return real 128x128 frames. |
| Does it pick up our modified tileset XML? | **YES.** `DATA\XML\TILESETS.XML` master list comes from CONFIG.MEG; each per-file XML is fetched through the mod-path overlay, so the mod's spliced `RA_TERRAIN_TEMPERATE.XML` (1.9 MB, TD blocks included) wins automatically once the mod folder is a registered mod path (`ModsToLoadRA` setting). |
| Do we have to hand-declare templates? | **Generated.** `tools/generate_tf_templates.py` emits `RedAlert/TemplateTypesTF.cs` (partial class, 325 declarations, ids 401–725) from the mod's `td_ra_tile_map.json` + the TD template tables (authentic land types/masks). Theatre availability is runtime-resolved from the tileset XML — no theatre data needed in declarations. |
| Classic iconsets? | The mod's `CCDATA/TFASSETS.MIX` contains classic `.tem` iconsets for ALL TD tiles (CRC-verified). The editor auto-loads mod `ccdata/*.mix`, and `TemplateType.Init` reads land types + usage masks from them (`InitFromClassicFileRa`), overriding the declaration defaults. Desert tiles in the Interior slot have no `.int` iconsets, so they fall back to the generated declaration data — fine. |
| `[TFTDTiles]` round-trip? | **Implemented** in `GamePluginRA`: `LoadTFTDTiles` (TD layer overrides template layer on load), `SaveMapPack` rewritten to split layers on save (TD cells → `[TFTDTiles]`, vanilla-safe name+size-gated fallback → `[MapPack]`, mirroring `td_map_to_ra.py`). Also added to the `extraIniText` scrub list so a stale copy is never preserved verbatim. Headless load/save probe was still running at write-up time — see "Verification state" below. |
| Native Linux (Track B)? | **Two viable paths, one surprise.** (a) The mono runtime path already works: the full WinForms editor builds and runs natively on Linux under mono — no port needed for a usable editor TODAY. (b) The net8/Avalonia port: the engine core (Model/Utility/RedAlert/TiberianDawn/Render/Interface + Event) compiles on net8 with only ~10 peripheral files excluded (Steam integration, WinForms marshaling helpers, tooltip/keyboard/MRU/ComboBox helpers) — 140 errors total, all UI/Steam leakage, zero in the file-format/map-logic code. **The surprise: `System.Drawing.Common` is Windows-only at RUNTIME on .NET 7+**, so the port must swap the imaging layer (SkiaSharp or KGySoft.Drawing) — that, plus the known ~90-file UI rewrite, is the real cost. Mono path costs none of that. |

## What was changed in the fork (uncommitted at write-up, commit follows)

**Linux buildability (`MobiusMapEditor.csproj`):**
- Windows-only pre/post-build events (`rd /s /q`) gated on `'$(OS)' == 'Windows_NT'`; POSIX
  equivalents added for other OSes.
- `Properties/Resources.resx`: 6 linked-file paths case-corrected (`..\resources\mmg1.wav` →
  `..\Resources\Mmg1.wav` etc.) — case-sensitive FS fix, no-op on Windows.
- `GenerateResourceUsePreserializedResources=true` + explicit `Reference` hints for
  `System.Resources.Extensions`, `Newtonsoft.Json`, `Pfim`, `System.Memory`, `System.Buffers`,
  `System.Runtime.CompilerServices.Unsafe`, `System.Numerics.Vectors` (non-Windows only; the
  old-style csproj + `dotnet msbuild` combo does not flow PackageReference assets).
- Build recipe: `export PATH="$HOME/.dotnet:$PATH"; export FrameworkPathOverride=/usr/lib/mono/4.7.2-api/;
  dotnet build MobiusMapEditor.sln -c Release`. Run: `mono CnCTDRAMapEditor/bin/Release/MobiusMapEditor.exe`
  (from a directory whose `DATA/CONFIG.MEG` test passes, or with `GameDirectoryPath` configured).

**Linux runtime (path semantics):**
- NEW `Utility/PlatformPath.cs` — `PlatformPath.Resolve()`: on non-Windows, normalises `\`→`/` and
  resolves each component case-insensitively against the disk; pass-through on Windows. Applied at
  every filesystem boundary in `MegafileManager` (loose/mod/archive lookups; also fixed an
  args-swapped `Path.Combine` bug in `FileExists`), `MixfileManager` (all 5 lookup sites),
  `StartupLoader.RemasterFileTest`.
- MEG-internal lookups normalised the other way (`ToUpper().Replace('/','\\')`) — MEG keys are
  uppercase-backslash (verified by dumping `TEXTURES_RA_SRGB.MEG`).
- `TilesetManager.LoadXmlfiles` and `TextureManager.GetTexture`: directory/name splitting now
  handles both separator styles (`Path.GetDirectoryName` returns "" for backslash paths on Linux —
  this was the "0 tilesets registered" and "art loads as dummy" bug pair).
- `Program.CopyLastUserConfig`: `Path.Combine` instead of literal `@"\"` concat + guard for a
  version folder without `user.config` (crashed at startup on Linux, latent bug on Windows too).

**Tiberian Factions awareness:**
- `RedAlert/TemplateTypes.cs` → `static partial class` (one-keyword diff).
- NEW GENERATED `RedAlert/TemplateTypesTF.cs` — 325 TD template declarations (ids 401–725).
- NEW `tools/generate_tf_templates.py` — the generator (re-run when the mod's tile map changes).
- `RedAlert/GamePluginRA.cs` — `LoadTFTDTiles` + layer-splitting `SaveMapPack` +
  `GetTFTDFallback` (name+size-gated vanilla fallback map) + `extraIniText` scrub entry.

## Verification state (both probes in scratchpad, rebuild + rerun as needed)

- `TrackAProbe.cs` (headless): 4 MEGs load; mod XML override live; `RA_Terrain_Temperate`
  registered; `tdsh10` frames=32 got=True **image=128x128**; `tdsh1` image=128x128; 729 template
  types (max id 725 + group dummies); `tdsh10.Init` → `ExistsInTheater=True`, real thumbnail.
  (`sh01`/`tdbridge1` "got=False" at icon 0 is correct — masked-out first icon.)
- `RoundTripProbe.cs` (headless): full plugin boot (classic init 0 errors — including the mod's
  TFASSETS.MIX via the ccdata mod path), loads a real converted map
  (`UGC_F1BE000000000006...MPR`), counts TD vs vanilla cells, saves, for byte-level diff of the
  re-encoded `[TFTDTiles]`/`[MapPack]` against the original via `td_map_to_ra.py`'s codec. Slow
  under mono (thousands of first-run art decodes); was still running at write-up time — check
  `scratchpad/roundtrip.log`. If the diff hasn't been done: decode both files' sections with
  `lcw_block_decompress`/`uublock_decode` from `scripts/td_map_to_ra.py` and compare cell arrays
  (icons of masked cells may legitimately re-encode as 0).

## GUI verification still to do (desktop session was password-locked overnight)

1. `mkdir -p ~/CnCRemastered/Mods/Red_Alert && ln -s <mod>/build/remaster/Vanilla_RA ~/CnCRemastered/Mods/Red_Alert/Vanilla_RA`
   (the editor's mod folder on mono resolves to `~/CnCRemastered\Mods` — PlatformPath handles the
   backslash; the folder-name lookup in `GetModPaths` needs the `ccmod.json` `game_type: RA`).
2. Set `ModsToLoadRA` to `Vanilla_RA` in the editor settings (Settings dialog, or user.config under
   `~/.local/share/Nyerguds/MobiusMapEditor.exe_Url_*/1.6.1.0/user.config`).
3. Launch from the game dir (`cd ~/.steam/steam/steamapps/common/CnCRemastered && mono <exe>`),
   open one of the mod's converted TD maps from `build/remaster/Vanilla_RA/CustomMaps/*.MPR`,
   confirm TD shores/bridges render, place a TD tile, save, and run the saved map through
   `td_map_to_ra.py inspect-ra` / in-game.
4. Wine is installed as a fallback runner (`wine <exe>`) if mono WinForms rendering misbehaves.

## Known gaps / next steps

- `sh17`/`sh18` exist in BOTH temperate and desert with the same id+name (fine — one declaration).
- Desert (interior-slot) tiles have no classic `.int` iconsets → land types come from generated
  declarations (TD-authentic) — acceptable.
- The `FixCorruptTiles` bridge auto-fix doesn't know TD bridges; harmless (skips unknown).
- Undo/redo, map preview rendering, and the template list UI all consume the same template/tileset
  model, so TD tiles should appear in the Templates tool automatically for the right theatres —
  NOT yet visually verified (locked session).
- Mono WinForms quirks (rendering, dialogs) unassessed beyond startup — evaluate before calling
  the mono path "the shipped editor".
- Upstream merge hygiene: all Linux fixes are surgical and Windows-safe; consider offering the
  PlatformPath/casing/resx fixes upstream to Nyerguds even though the TF fork won't be PR'd.
