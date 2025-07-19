using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace LethalScanCommand;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("baer1.ChatCommandAPI")]
public class LethalScanCommand : BaseUnityPlugin
{
    public static LethalScanCommand Instance { get; private set; } = null!;
    internal static new ManualLogSource Logger { get; private set; } = null!;
    internal static Harmony? Harmony { get; set; }

    public enum ApproximateValueOnCruiserOptions
    {
        Never,
        UnMagneted,
        Always,
    }

    private ConfigEntry<bool>? approximateValueInShip;
    public static bool ApproximateValueInShip =>
        Instance != null && Instance.approximateValueInShip is { Value: true };
    private ConfigEntry<bool>? approximateValueOnShip;
    public static bool ApproximateValueOnShip =>
        Instance != null && Instance.approximateValueOnShip is { Value: true };
    private ConfigEntry<ApproximateValueOnCruiserOptions>? approximateValueOnCruiser;
    public static ApproximateValueOnCruiserOptions ApproximateValueOnCruiser =>
        Instance == null || Instance.approximateValueOnCruiser is null
            ? ApproximateValueOnCruiserOptions.UnMagneted
            : Instance.approximateValueOnCruiser.Value;
    private ConfigEntry<bool>? approximateValueOutsideShip;
    public static bool ApproximateValueOutsideShip =>
        Instance == null || Instance.approximateValueOutsideShip is { Value: true } or null;

    private ConfigEntry<bool>? compactResponse;
    public static bool CompactResponse =>
        Instance == null || Instance.compactResponse is { Value: true } or null;

    private void Awake()
    {
        Logger = base.Logger;
        Instance = this;

        approximateValueInShip = Config.Bind(
            "General",
            "ApproximateValueInShip",
            false,
            "Whether to approximate the value of the items inside the ship"
        );
        approximateValueOnShip = Config.Bind(
            "General",
            "ApproximateValueOnShip",
            false,
            "Whether to approximate the value of the items on the ship (not collected)"
        );
        approximateValueOnCruiser = Config.Bind(
            "General",
            "ApproximateValueOnCruiser",
            ApproximateValueOnCruiserOptions.UnMagneted,
            "When to approximate the value of the items on the company cruiser"
        );
        approximateValueOutsideShip = Config.Bind(
            "General",
            "ApproximateValueOutsideShip",
            true,
            "Whether to approximate the value of the items outside the ship (not collected)"
        );
        compactResponse = Config.Bind(
            "General",
            "CompactResponse",
            true,
            "Whether to reply with a short summary or a complete sentence"
        );

        _ = new ScanCommand();

        Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);
        Logger.LogDebug("Patching...");
        Harmony.PatchAll();
        Logger.LogDebug("Finished patching!");

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
    }
}
