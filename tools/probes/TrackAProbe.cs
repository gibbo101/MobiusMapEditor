using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MobiusEditor;
using MobiusEditor.Model;
using MobiusEditor.RedAlert;
using MobiusEditor.Utility;

class TrackAProbe
{
    static void Main(string[] args)
    {
        string gameDir = "/home/gibbo101/.steam/steam/steamapps/common/CnCRemastered";
        string modDir = "/home/gibbo101/Documents/development/cnc-remastered-mods/cnc-ra-tiberian-factions/build/remaster/Vanilla_RA";
        var modPaths = new Dictionary<GameType, string[]> { { GameType.RedAlert, new[] { modDir } } };
        var gameFolders = new Dictionary<GameType, string>();
        var mfm = new MegafileManager(Path.Combine(gameDir, "DATA"), gameDir, modPaths, null, gameFolders);
        foreach (string meg in new[] { "CONFIG.MEG", "TEXTURES_COMMON_SRGB.MEG", "TEXTURES_SRGB.MEG", "TEXTURES_RA_SRGB.MEG" })
        {
            Console.WriteLine("load " + meg + ": " + mfm.LoadArchive(meg));
        }
        mfm.Reset(GameType.RedAlert, null);

        // 1. Does the mod's tileset XML override get picked up?
        using (Stream s = mfm.OpenFile(@"DATA\XML\TILESETS\RA_TERRAIN_TEMPERATE.XML"))
        {
            if (s == null) { Console.WriteLine("TEMPERATE XML: NOT FOUND"); }
            else
            {
                using (var r = new StreamReader(s))
                {
                    string xml = r.ReadToEnd();
                    Console.WriteLine("TEMPERATE XML: " + xml.Length + " bytes, contains TDSH10: " + xml.Contains("TDSH10"));
                }
            }
        }

        // 2. Full tileset manager chain.
        var tsm = new TilesetManager(mfm, Globals.TilesetsXMLPath, Globals.TexturesPath);
        tsm.Reset(GameType.RedAlert, TheaterTypes.Temperate);
        Console.WriteLine("RA_Terrain_Temperate exists: " + tsm.TilesetExists("RA_Terrain_Temperate"));
        foreach (string tile in new[] { "sh01", "tdsh10", "tdsh1", "tdbridge1" })
        {
            int len = tsm.GetTileDataLength(tile);
            bool ok = tsm.GetTileData(tile, 0, out Tile t, false, false);
            var img = ok && t != null ? t.Image : null;
            Console.WriteLine(tile + ": frames=" + len + " got=" + ok + " image=" + (img == null ? "null" : img.Width + "x" + img.Height));
        }

        // 3. Template declarations present and initialisable?
        var all = TemplateTypes.GetTypes().ToList();
        Console.WriteLine("RA template count: " + all.Count + " (max id " + all.Max(tt => tt.ID) + ")");
        var tdsh10 = all.FirstOrDefault(tt => tt.Name == "tdsh10");
        Console.WriteLine("tdsh10 declared: " + (tdsh10 != null));
        if (tdsh10 != null)
        {
            Globals.TheTilesetManager = tsm;
            Globals.TheArchiveManager = mfm;
            var gameInfo = new GameInfoRedAlert();
            tdsh10.Init(gameInfo, TheaterTypes.Temperate, false);
            Console.WriteLine("tdsh10 Init: ExistsInTheater=" + tdsh10.ExistsInTheater
                + " thumb=" + (tdsh10.Thumbnail == null ? "null" : tdsh10.Thumbnail.Width + "x" + tdsh10.Thumbnail.Height)
                + " lands=" + tdsh10.GetLandTypesString('_', ' '));
        }
        Console.WriteLine("PROBE COMPLETE");
    }
}
