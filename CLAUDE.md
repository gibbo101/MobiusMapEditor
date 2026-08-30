# Mobius Map Editor fork — Claude context

Luke's fork of Nyerguds' Mobius Map Editor (`origin` = `gibbo101/MobiusMapEditor`, `upstream` =
Nyerguds — pull bugfixes only, **never PR upstream**; this fork is mod-exclusive and diverges
hard). Purpose: a map editor that understands the Tiberian Factions mod
(`../cnc-ra-tiberian-factions/`) so Luke can author skirmish + campaign maps directly.

Workspace-wide rules (Deck deploys, memory, don't-dos) live in `../CLAUDE.md` — read it too.

## Read first, in this order

1. `LINUX_EDITOR_PLAN.md` — **the live decision record (2026-08-23).** Build a Linux-native
   editor reusing the net8-clean core (formats, codecs, map model, our TF tables) and rewrite the
   WinForms shell (Avalonia, docked panels, headless CLI for agent-driven trigger authoring).
   Status: DECIDED, NOT STARTED. Order of work: **renderer spike**
   (map → PNG on net8 without `System.Drawing`; the one gating unknown) → only then scaffold.
2. `NATIVE_PORT_HANDOVER.md` — state of the *existing* mono-run WinForms fork: mod-aware tiles
   shipped (60 TF entities, 31/31 CustomMaps round-trip byte-identical), mono quirk fixes, and the
   open blocker (tool dialogs never map on screen, `IsUnMapped`). Diagnosis at its top.
3. `tools/probes/README.md` — headless probe harness; `RenderProbe.cs` is the reference for the
   renderer spike.

## Environment (already set up)

- .NET SDKs 8.0.421 + 10.0.302 in `~/.dotnet` — `export PATH="$HOME/.dotnet:$PATH"`.
- Avalonia templates installed and build-verified on this box.
- mono + msbuild installed (for the current net4.7.2 fork): `./run-editor.sh` launches it;
  logs to `/tmp/mobius-editor.log`.
- Wine 9.0 is installed but **Wine is not part of the plan** (decided 2026-08-30: build native).
- Pristine upstream reference: `../reference/MobiusMapEditor/` (read-only). EA's original editor
  source ships in `~/.steam/steam/steamapps/common/CnCRemastered/SOURCECODE/CnCTDRAMapEditor/`.

## Rules specific to this repo

- Mod tables (`OverlayTypes`, `BuildingTypesTF`, `TemplateTypesTF`) are hand-transcribed from
  the mod's `redalert/` C++ and **drift** — verify against the DLL source before trusting them,
  and generate rather than transcribe when the new core is built.
- Triggers/TeamTypes are in scope from day one of the new editor (17 campaign missions depend on
  them) — do not descope them.
- Licence: GPL v3 + EA's Section 7 terms; keep upstream attribution.
- No `Co-Authored-By` trailers on commits.
