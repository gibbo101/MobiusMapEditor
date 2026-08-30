# Mobius Map Editor fork — Claude context

Luke's fork of Nyerguds' Mobius Map Editor (`origin` = `gibbo101/MobiusMapEditor`, `upstream` =
Nyerguds — pull bugfixes only, **never PR upstream**; this fork is mod-exclusive and diverges
hard). Purpose: a map editor that understands the Tiberian Factions mod
(`../cnc-ra-tiberian-factions/`) so Luke can author skirmish + campaign maps directly.

Workspace-wide rules (Deck deploys, memory, don't-dos, screenshot loop) live in `../CLAUDE.md` —
read it too. Luke is on JetBrains (Rider for C#); don't scaffold `.vscode/`.

## Read first, in this order

1. `LINUX_EDITOR_PLAN.md` — **the live decision record (2026-08-23, amended 2026-08-30).** Build
   a Linux-native editor reusing the net8-clean core (formats, codecs, map model, our TF tables)
   and rewrite the WinForms shell (Avalonia, docked panels, headless CLI for agent-driven trigger
   authoring). Status: DECIDED, NOT STARTED. Wine is **not** part of the plan. Order of work:
   **renderer spike** (map → PNG on net8 without `System.Drawing`; the one gating unknown) →
   only then scaffold.
2. `NATIVE_PORT_HANDOVER.md` — state of the *existing* mono-run WinForms fork: mod-aware tiles
   shipped (60 TF entities, 31/31 CustomMaps round-trip byte-identical), mono quirk tally, where
   the official maps live, and the open blocker (tool dialogs never map on screen, `IsUnMapped`).
3. `tools/probes/README.md` — headless probe harness; `RenderProbe.cs` is the reference for the
   renderer spike.

## Layout

```
mobius-editor/
├── CnCTDRAMapEditor/      ← the editor. Model/ Utility/ RedAlert/ TiberianDawn/ Render/ Interface/
│                            = portable core (net8-clean). Dialogs/ Controls/ Tools/ Widgets/
│                            MainForm = WinForms shell (to be replaced).
├── tools/
│   ├── generate_tf_templates.py  ← generates RedAlert/TemplateTypesTF.cs from the mod's
│   │                                scripts/td_ra_tile_map.json (the drift-proof pattern)
│   └── probes/                   ← headless C# harnesses + sweep_maps.sh regression sweep
├── _prebuilt/             ← upstream 1.6.1.0 exe, git-excluded, reference only
├── LINUX_EDITOR_PLAN.md · NATIVE_PORT_HANDOVER.md · MANUAL.md (upstream user manual)
└── run-editor.sh          ← launches the mono build from the game dir
```

The new native editor will live in this repo alongside the old tree (decide the folder name
when scaffolding; keep the old tree buildable until the new one reaches parity).

## Branches

- `master` — tracks upstream Nyerguds (`develop` merges). Pristine-ish; don't develop here.
- `tf-mod-aware` — **our working branch**, all mod-aware work + docs. Branch feature work off
  it and merge back; no PR ceremony needed (solo repo), but keep commits small and well-explained.

## Build / run / test (current mono fork)

```bash
export PATH="$HOME/.dotnet:$PATH"
export FrameworkPathOverride=/usr/lib/mono/4.7.2-api/
dotnet build MobiusMapEditor.sln -c Release     # .NET SDK 8.0.421 / 10.0.302 in ~/.dotnet
./run-editor.sh                                  # mono, cd's to the game dir first; log → /tmp/mobius-editor.log
```

- Probes: see `tools/probes/README.md` (`mono-csc` against the built exe). Reset the megafile
  AND tileset managers before `plugin.Load` or a headless run blocks on a MessageBox forever.
- Regression sweep: `tools/probes/sweep_maps.sh` — all 31 mod CustomMaps must round-trip
  byte-identical (`[TFTDTiles]` + `[MapPack]` diffed with the mod's `td_map_to_ra.py` codec).
- Wine 9.0 is installed but out of scope — don't reach for it.

## Testing — TDD for the native editor (rule, not a suggestion)

The native editor is a multi-week build over a core that is pure functions over bytes and a map
model. That is ideal TDD territory and the plan depends on it:

- **Test-first for everything in the core:** codecs (LCW, UUBlock, MIX/MEG readers), the
  `[TFTDTiles]`/`[MapPack]` round-trip, template/overlay/building tables, placement + occupancy
  rules, trigger/teamtype serialisation, the headless CLI commands. Write the failing test, then
  the code.
- **Oracles already exist:** the 124 official maps in `MAIN.MIX → general.mix`, the mod's 31
  CustomMaps, the mono editor's own output, and `RenderProbe`'s PNG. Golden-file tests against
  these are the backbone: byte-identical round-trip, pixel-compare renders (tolerance only where
  a codec swap justifies it, and say why).
- **Mod-table drift is a test**, not a hope: the tables must be generated from `redalert/` or
  validated against it, and a test fails on mismatch.
- **Don't unit-test the GUI pixels;** test the view-model/command layer the GUI calls, and
  verify the canvas by eye (screenshot loop). Every GUI action should be callable from the
  headless CLI, which is what makes it testable.
- **Red → green, always (Luke, 2026-08-30):** write the failing test, run it and see it fail,
  then make the smallest code change that turns it green. A test that passes on first run is
  not evidence of anything — check it actually exercises the change.
- Framework: **xUnit on net8**, `dotnet test`. Keep the suite fast enough to run on every commit.

## Rules specific to this repo

- Mod tables (`OverlayTypes`, `BuildingTypesTF`, `TemplateTypesTF`) are hand-transcribed from
  the mod's `redalert/` C++ and **drift** — verify against the DLL source before trusting them,
  and generate rather than transcribe when the new core is built.
- Triggers/TeamTypes are in scope from day one of the new editor (17 campaign missions depend on
  them) — do not descope them.
- **Don't reimplement the correctness that lives in `Model/`** (bibs, occupancy, placement,
  house colours). Reuse it; rewrite only the shell.
- **Codec single source of truth:** the editor core is the reference LCW/UUBlock implementation.
  `td_map_to_ra.py` and any Python decode are verification tools, never a second writer.
- ⚠️ **Data-loss hazard with the old editor:** if it logs "unknown structure" on load it will
  drop those objects on save. Only Save-As to scratch names on any map that warns.
- **Never save over shipped mod maps or official maps** without a byte-diff of the result first.
- Officially shipped RA maps are `scmNNea.ini` in `MAIN.MIX → general.mix` (unencrypted extended
  MIX; details in the handover). Delivery = shadow by name from the mod's `CCDATA/`.
- Licence: GPL v3 + EA's Section 7 terms; keep upstream attribution. Anything reusing the core
  inherits it.
- No `Co-Authored-By` trailers on commits. Commit messages say why.
- Comments describe expected behaviour only — no ticket/history references in source.

## Collaboration

- Push back when something looks wrong; Luke would rather hear it than have it agreed with.
- Prove the one hard thing in isolation before building around it (renderer spike, same as the
  TS asset and desert theatre spikes in the mod). Record falsified theories in the docs so they
  don't get re-chased.
- Keep the docs above current: `LINUX_EDITOR_PLAN.md` is the decision record, the handover is
  the state record. Update them at the end of any session that changes either.
