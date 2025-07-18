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

    private ConfigEntry<bool>? approximateValue;
    public static bool ApproximateValue =>
        Instance == null || Instance.approximateValue is { Value: true } or null;

    private ConfigEntry<bool>? compactResponse;
    public static bool CompactResponse =>
        Instance == null || Instance.compactResponse is { Value: true } or null;

    private void Awake()
    {
        Logger = base.Logger;
        Instance = this;

        approximateValue = Config.Bind(
            "General",
            "ApproximateValue",
            true,
            "Whether to display an approximation of the value or the exact amount"
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
