using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using MobiusEditor;
using MobiusEditor.Interface;
using MobiusEditor.Model;
using MobiusEditor.RedAlert;
using MobiusEditor.Render;
using MobiusEditor.Utility;

// Headless canvas render: loads a map through the full plugin and draws the painted map
// layers via MapRenderer to a PNG, reproducing the GUI canvas exactly. Verification target
// for template/overlay art bugs (animated-tile frames, overlay table) without a display.
class RenderProbe
{
    static void Main(string[] args)
    {
        string gameDir = "/home/gibbo101/.steam/steam/steamapps/common/CnCRemastered";
        string modDir = "/home/gibbo101/Documents/development/cnc-remastered-mods/cnc-ra-tiberian-factions/build/remaster/Vanilla_RA";
        string mapIn = args.Length > 0 ? args[0]
            : modDir + "/CustomMaps/UGC_F1BE000000000006_0000000000000006_MAPDATA.MPR";
        string pngOut = args.Length > 1 ? args[1] : "/tmp/map-render.png";
        double tileScale = args.Length > 2 ? double.Parse(args[2]) : 0.25;

        var modPaths = new Dictionary<GameType, string[]> { { GameType.RedAlert, new[] { modDir } } };
        var gameFolders = new Dictionary<GameType, string>();
        var gameTypeInfo = GameTypeFactory.GetGameInfos();
        foreach (GameInfo gi in gameTypeInfo)
        {
            if (gi != null) { gameFolders[gi.GameType] = gi.ClassicFolderRemasterData; }
        }
        var mfm = new MegafileManager(Path.Combine(gameDir, "DATA"), gameDir, modPaths, null, gameFolders);
        foreach (string meg in new[] { "CONFIG.MEG", "TEXTURES_COMMON_SRGB.MEG", "TEXTURES_SRGB.MEG", "TEXTURES_RA_SRGB.MEG" })
        {
            mfm.LoadArchive(meg);
        }
        List<string> le = new List<string>(), fle = new List<string>();
        foreach (GameInfo gi in gameTypeInfo)
        {
            if (gi != null) { gi.InitClassicFiles(mfm.ClassicFileManager, le, fle, true); }
        }
        Console.WriteLine("classic init errors: " + le.Count + " / " + fle.Count);
        // Reset managers to the MAP's theater (desert = Interior slot), matching RoundTripProbe.
        string mapText = File.ReadAllText(mapIn);
        TheaterType mapTheater = mapText.Contains("Theater=Interior") ? TheaterTypes.Interior
            : mapText.Contains("Theater=Snow") ? TheaterTypes.Snow : TheaterTypes.Temperate;
        mfm.Reset(GameType.RedAlert, mapTheater);
        Globals.TheArchiveManager = mfm;
        var tsm = new TilesetManager(mfm, Globals.TilesetsXMLPath, Globals.TexturesPath);
        tsm.Reset(GameType.RedAlert, mapTheater);
        Globals.TheTilesetManager = tsm;
        Globals.TheShapeCacheManager = new ShapeCacheManager();
        Globals.TheTeamColorManager = new TeamColorManager(mfm);
        string gameTextFilename = String.Format(Globals.GameTextFilenameFormat, "EN-US");
        var gtm = new GameTextManager(Globals.TheArchiveManager, gameTextFilename);
        Globals.TheGameTextManager = gtm;
        foreach (GameInfo gi in gameTypeInfo)
        {
            if (gi == null) continue;
            foreach (TheaterType theater in gi.AllTheaters)
            {
                theater.IsRemasterTilesetFound = tsm.TilesetExists(theater.MainTileset);
            }
        }

        GameInfo gameInfo = new GameInfoRedAlert();
        IGamePlugin plugin = gameInfo.CreatePlugin(false, true);
        FileType ft = FileType.INI;
        string[] errors = plugin.Load(mapIn, mapIn, File.ReadAllBytes(mapIn), null, null, ref ft).ToArray();
        Console.WriteLine("load errors: " + errors.Length);

        // Report multiplier resolution on animated vs static TF templates.
        foreach (string tmpl in new[] { "tdsh1", "tdsh10", "tdriv1", "tdd07", "tdclear1" })
        {
            TemplateType tt = plugin.Map.TemplateTypes.FirstOrDefault(t => t.Name == tmpl);
            if (tt == null) { Console.WriteLine(tmpl + ": not in list"); continue; }
            Console.WriteLine(String.Format("{0}: icons={1} tilesLen={2} shapeIdx(1)={3} exists={4}",
                tmpl, tt.NumIcons, Globals.TheTilesetManager.GetTileDataLength(tmpl), tt.GetShapeIndex(1), tt.ExistsInTheater));
        }

        int w = plugin.Map.Metrics.Width, h = plugin.Map.Metrics.Height;
        Size tileSize = new Size((int)Math.Round(Globals.OriginalTileWidth * tileScale), (int)Math.Round(Globals.OriginalTileHeight * tileScale));
        using (Bitmap bm = new Bitmap(w * tileSize.Width, h * tileSize.Height))
        {
            bm.SetResolution(96, 96);
            using (Graphics g = Graphics.FromImage(bm))
            {
                MapRenderer.Render(gameInfo, plugin.Map, g, null, MapLayerFlag.MapLayers, tileScale, false, Globals.TheShapeCacheManager);
            }
            bm.Save(pngOut, ImageFormat.Png);
        }
        Console.WriteLine("rendered " + w + "x" + h + " cells at " + tileSize.Width + "px -> " + pngOut);
        Console.WriteLine("RENDER COMPLETE");
    }
}
