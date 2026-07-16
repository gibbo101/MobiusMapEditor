# Headless verification probes (Linux, mono)

Console harnesses that exercise the editor's engine core without the GUI. Used to verify the
Tiberian Factions integration overnight on 2026-07-16; keep them runnable.

Build and run (after building the editor):

```bash
REL=../../CnCTDRAMapEditor/bin/Release
mono-csc -r:$REL/MobiusMapEditor.exe -r:System.Drawing.dll -out:/tmp/probe.exe <Probe>.cs
cd $REL && MONO_PATH=$REL:$REL/bin mono /tmp/probe.exe
```

- `TrackAProbe.cs` - mod-path overlay, tileset XML override, TD tile art loading (expects real
  128x128 frames for tdsh10/tdsh1).
- `InitProbe.cs` - Init()s every template type for a theater, reports ExistsInTheater counts
  (Temperate 419 / Interior-desert 308 as of writing). Edit the `theater` variable to switch.
- `RoundTripProbe.cs` - full plugin load of a converted TD map + save; diff the [TFTDTiles] and
  [MapPack] cell arrays against the original with the mod's td_map_to_ra.py codec (expect 0/0).
- `RenderProbe.cs` - full plugin load + MapRenderer draw of the painted map layers to a PNG
  (`/tmp/map-render.png`), reproducing the GUI canvas without a display. Args: [map] [png] [scale].
  Also prints shape-multiplier resolution for sample TF templates (animated tiles expect
  shapeIdx(1)=8 in-theater).

Gotcha: Reset the megafile AND tileset managers to (RedAlert, <theater>) BEFORE plugin.Load, or
Map.InitTheater's catch pops a MessageBox that blocks forever on a headless/locked display.
Game and mod paths are hardcoded for this machine at the top of each probe.
