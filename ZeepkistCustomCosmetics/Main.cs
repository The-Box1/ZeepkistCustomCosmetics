using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace ZeepkistCustomCosmetics;

[BepInAutoPlugin("dev.thebox1.zeepkistimporter")]
[BepInProcess("Zeepkist.exe")]
public partial class Main : BaseUnityPlugin
{
    public new static BepInEx.Logging.ManualLogSource Logger;

    private Harmony Harmony { get; } = new (Id);

    public static ConfigEntry<int> lastSelectedGameZeepkist;
    public static ConfigEntry<int> lastSelectedGameHat;

    private static string assetPath = Path.Combine(Paths.GameRootPath, "Zeepkist_Data", "Cosmetics");
    public static string zeepkistPath = Path.Combine(assetPath, "Zeepkists");
    public static string hatPath = Path.Combine(assetPath, "Hats");

    public static List<int> newZeepkists = new();
    public static List<int> newHats = new();

    public void Awake()
    {
        Directory.CreateDirectory(zeepkistPath);
        Directory.CreateDirectory(hatPath);
        
        lastSelectedGameZeepkist = Config.Bind("General", "LastSelectedGameZeepkist", -1, "Holds the last normal zeepkist that was selected");
        lastSelectedGameHat = Config.Bind("General", "LastSelectedGameHat", -1, "Holds the last normal hat that was selected");

        Logger = base.Logger;
        Harmony.PatchAll();
        Logger.LogMessage("Loaded ZeepkistImporter");
    }
}