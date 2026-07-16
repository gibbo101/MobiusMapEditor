using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MobiusEditor;
using MobiusEditor.Model;
using MobiusEditor.RedAlert;
using MobiusEditor.Utility;

class InitProbe
{
    static void Main(string[] args)
    {
        string gameDir = "/home/gibbo101/.steam/steam/steamapps/common/CnCRemastered";
        string modDir = "/home/gibbo101/Documents/development/cnc-remastered-mods/cnc-ra-tiberian-factions/build/remaster/Vanilla_RA";
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
        mfm.Reset(GameType.RedAlert, null);
        Globals.TheArchiveManager = mfm;
        var tsm = new TilesetManager(mfm, Globals.TilesetsXMLPath, Globals.TexturesPath);
        Globals.TheTilesetManager = tsm;
        Globals.TheShapeCacheManager = new ShapeCacheManager();
        Globals.TheTeamColorManager = new TeamColorManager(mfm);
        var gameInfo = new GameInfoRedAlert();
        var theater = TheaterTypes.Interior;
        tsm.Reset(GameType.RedAlert, theater);
        mfm.Reset(GameType.RedAlert, theater);
        int done = 0, failed = 0, exists = 0;
        DateTime start = DateTime.Now;
        foreach (TemplateType tt in TemplateTypes.GetTypes())
        {
            try
            {
                tt.Init(gameInfo, theater, Globals.FilterTheaterObjects);
                done++;
                if (tt.ExistsInTheater) exists++;
            }
            catch (Exception ex)
            {
                failed++;
                Console.WriteLine("INIT FAIL " + tt.Name + " (id " + tt.ID + "): " + ex.GetType().Name + ": " + ex.Message);
                if (failed <= 3) Console.WriteLine(ex.StackTrace);
            }
        }
        Console.WriteLine("Init done=" + done + " failed=" + failed + " existsInTheater=" + exists
            + " elapsed=" + (DateTime.Now - start).TotalSeconds.ToString("F1") + "s");
        Console.WriteLine("INITPROBE COMPLETE");
    }
}
