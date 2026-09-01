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
using MobiusEditor.Utility;

// Headless object-catalog check: boots the RA plugin (which Inits all types for the
// theater), then dumps every Tiberian Factions building/unit/infantry thumbnail onto
// a labeled contact sheet (/tmp/tf-objects.png) and prints per-type status. Any type
// whose art failed to resolve shows up as missing/dummy here without needing the GUI.
class ObjectProbe
{
    static void Main(string[] args)
    {
        string gameDir = "/home/gibbo101/.steam/steam/steamapps/common/CnCRemastered";
        string modDir = "/home/gibbo101/Documents/development/cnc-remastered-mods/cnc-ra-tiberian-factions/build/remaster/Vanilla_RA";
        string mapIn = modDir + "/CustomMaps/UGC_F1BE000000000006_0000000000000006_MAPDATA.MPR";
        string pngOut = args.Length > 0 ? args[0] : "/tmp/tf-objects.png";

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
        mfm.Reset(GameType.RedAlert, TheaterTypes.Temperate);
        Globals.TheArchiveManager = mfm;
        var tsm = new TilesetManager(mfm, Globals.TilesetsXMLPath, Globals.TexturesPath);
        tsm.Reset(GameType.RedAlert, TheaterTypes.Temperate);
        Globals.TheTilesetManager = tsm;
        Globals.TheShapeCacheManager = new ShapeCacheManager();
        Globals.TheTeamColorManager = new TeamColorManager(mfm);
        string gameTextFilename = String.Format(Globals.GameTextFilenameFormat, "EN-US");
        Globals.TheGameTextManager = new GameTextManager(Globals.TheArchiveManager, gameTextFilename);
        foreach (GameInfo gi2 in gameTypeInfo)
        {
            if (gi2 == null) continue;
            foreach (TheaterType theater in gi2.AllTheaters)
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
        plugin.Load(mapIn, mapIn, File.ReadAllBytes(mapIn), null, null, ref ft);

        var entries = new List<(string label, Bitmap thumb)>();
        foreach (BuildingType b in plugin.Map.BuildingTypes.Where(b => b.ID >= 87).OrderBy(b => b.ID))
        {
            Console.WriteLine(String.Format("BLDG {0,-10} exists={1} thumb={2} name=\"{3}\"", b.Name, b.ExistsInTheater, b.Thumbnail != null, b.DisplayName));
            entries.Add((b.Name, b.Thumbnail));
        }
        foreach (UnitType u in plugin.Map.AllUnitTypes.Where(u => (u.IsGroundUnit && u.ID >= 22) || (u.IsAircraft && u.ID >= 7) || (u.IsVessel && u.ID >= 7)).OrderBy(u => u.IsGroundUnit ? 0 : u.IsAircraft ? 1 : 2).ThenBy(u => u.ID))
        {
            Console.WriteLine(String.Format("UNIT {0,-10} thumb={1} name=\"{2}\"", u.Name, u.Thumbnail != null, u.DisplayName));
            entries.Add((u.Name, u.Thumbnail));
        }
        foreach (InfantryType i in plugin.Map.AllInfantryTypes.Where(i => i.ID >= 26).OrderBy(i => i.ID))
        {
            Console.WriteLine(String.Format("INF  {0,-10} thumb={1} name=\"{2}\"", i.Name, i.Thumbnail != null, i.DisplayName));
            entries.Add((i.Name, i.Thumbnail));
        }

        const int cell = 200, cols = 8, labelH = 16;
        int rows = (entries.Count + cols - 1) / cols;
        using (Bitmap sheet = new Bitmap(cols * cell, rows * (cell + labelH)))
        using (Graphics g = Graphics.FromImage(sheet))
        using (Font font = new Font(FontFamily.GenericSansSerif, 9))
        {
            g.Clear(Color.DimGray);
            for (int n = 0; n < entries.Count; ++n)
            {
                int x = (n % cols) * cell, y = (n / cols) * (cell + labelH);
                if (entries[n].thumb != null)
                {
                    Bitmap t = entries[n].thumb;
                    double scale = Math.Min((double)cell / t.Width, (double)cell / t.Height);
                    scale = Math.Min(scale, 2.0);
                    int w = (int)(t.Width * scale), h = (int)(t.Height * scale);
                    g.DrawImage(t, x + (cell - w) / 2, y + (cell - h) / 2, w, h);
                }
                else
                {
                    g.DrawString("NO ART", font, Brushes.Red, x + 60, y + 90);
                }
                g.DrawString(entries[n].label, font, Brushes.White, x + 4, y + cell);
            }
            sheet.Save(pngOut, ImageFormat.Png);
        }
        Console.WriteLine("sheet: " + pngOut + " (" + entries.Count + " objects)");
        Console.WriteLine("OBJECT PROBE COMPLETE");
    }
}
