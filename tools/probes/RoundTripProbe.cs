using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MobiusEditor;
using MobiusEditor.Interface;
using MobiusEditor.Model;
using MobiusEditor.RedAlert;
using MobiusEditor.Utility;

class RoundTripProbe
{
    static void Main(string[] args)
    {
        string gameDir = "/home/gibbo101/.steam/steam/steamapps/common/CnCRemastered";
        string modDir = "/home/gibbo101/Documents/development/cnc-remastered-mods/cnc-ra-tiberian-factions/build/remaster/Vanilla_RA";
        string mapIn = args.Length > 0 ? args[0]
            : modDir + "/CustomMaps/UGC_F1BE000000000006_0000000000000006_MAPDATA.MPR";
        string mapOut = args.Length > 1 ? args[1] : "/tmp/roundtrip-out.mpr";

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
        // Reset managers to the MAP's theater, not blindly Temperate — desert maps live in the
        // Interior slot and their desert-only templates vanish if the wrong tileset is loaded.
        string mapText = File.ReadAllText(mapIn);
        TheaterType mapTheater = mapText.Contains("Theater=Interior") ? TheaterTypes.Interior
            : mapText.Contains("Theater=Snow") ? TheaterTypes.Snow : TheaterTypes.Temperate;
        Console.WriteLine("theater: " + mapTheater.Name);
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

        IGamePlugin plugin = new GameInfoRedAlert().CreatePlugin(false, true);
        // Match the GUI's LoadNewPlugin: Initialize loads RULES.INI layers and the
        // team colors, so renders carry house remaps exactly like the editor canvas.
        plugin.Initialize();
        plugin.Map.FlagColors = plugin.GetFlagColors();
        FileType ft = FileType.INI;
        string[] errors = plugin.Load(mapIn, mapIn, File.ReadAllBytes(mapIn), null, null, ref ft).ToArray();
        Console.WriteLine("load errors: " + errors.Length);
        foreach (string e in errors.Take(8)) Console.WriteLine("  " + e);
        int tdCells = 0, vanillaCells = 0;
        var counts = new Dictionary<string, int>();
        for (int y = 0; y < plugin.Map.Metrics.Height; ++y)
        {
            for (int x = 0; x < plugin.Map.Metrics.Width; ++x)
            {
                var t = plugin.Map.Templates[y, x];
                if (t == null) continue;
                if (t.Type.ID >= 401) { tdCells++; counts[t.Type.Name] = counts.ContainsKey(t.Type.Name) ? counts[t.Type.Name] + 1 : 1; }
                else vanillaCells++;
            }
        }
        Console.WriteLine("TD cells: " + tdCells + ", vanilla cells: " + vanillaCells);
        foreach (var kv in counts.OrderByDescending(kv => kv.Value).Take(8))
            Console.WriteLine("  " + kv.Key + ": " + kv.Value);
        plugin.Save(mapOut, FileType.INI);
        Console.WriteLine("saved to " + mapOut);
        Console.WriteLine("ROUNDTRIP COMPLETE");
    }
}
