using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using Microsoft.VisualBasic;
using UnityEngine;

namespace AutoScan;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class AutoScan : BaseUnityPlugin
{
    public static AutoScan Instance { get; private set; } = null!;
    internal static new ManualLogSource Logger { get; private set; } = null!;
    internal static Harmony? Harmony { get; set; }
    private ConfigEntry<bool>? autoAnnounce;
    public static bool AutoAnnounce => Instance != null && Instance.autoAnnounce is { Value: true };

    private ConfigEntry<bool>? announceOutsideLoot;
    public static bool AnnounceOutsideLoot =>
        Instance != null && Instance.announceOutsideLoot is { Value: true };

    private ConfigEntry<bool>? announceValue;
    public static bool AnnounceValue =>
        Instance != null && Instance.announceValue is { Value: true };

    private ConfigEntry<bool>? hostOnly;
    public static bool HostOnly => Instance != null && Instance.hostOnly is { Value: true };
    private void Awake()
    {
        Logger = base.Logger;
        Instance = this;
        autoAnnounce = Config.Bind(
            "General",
            "AutoAnnounce",
            true,
            "Automatically announce the number of items outside the ship at the start of the round"
        );
        announceOutsideLoot = Config.Bind(
            "General",
            "AnnounceOutsideLoot",
            true,
            "Announce the amount of loot outside the facility including Beehives and Sapsucker eggs"
        );
        announceValue = Config.Bind(
            "General",
            "AnnounceValue",
            true,
            "Announce the total loot value when scanning"
        );
        hostOnly = Config.Bind(
            "General",
            "HostOnly",
            true,
            "Only announce if you are the host"
        );

        Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);
        Logger.LogDebug("Patching...");
        Harmony.PatchAll();
        Logger.LogDebug("Finished patching!");

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");

    }
    public static void ReloadConfig()
    {
        if (BepInEx.Bootstrap.Chainloader.ManagerObject == null)
            return;

        var pluginInstance = BepInEx.Bootstrap.Chainloader.ManagerObject.GetComponent<AutoScan>();
        if (pluginInstance == null)
            return;

        pluginInstance.Config.Reload();
    }

    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.openingDoorsSequence))]
    internal class AutoAnnouncePatch
    {
        public static bool Countitems(
            out string? error,
            out int outsideShip,
            out int outsideShipBees,
            out int outsideShipValueNoBees,
            out int outsideShipValueBees
        )
        {
            outsideShip = 0;
            outsideShipBees = 0;
            outsideShipValueNoBees = 0;
            outsideShipValueBees = 0;

            error = "items is null";
            var items = Object.FindObjectsOfType<GrabbableObject>();
            if (items == null)
                return false;

            error = "ship is null";
            var ship = GameObject.Find("Environment/HangarShip");
            if (ship == null)
                return false;

            error = "vehicles is null";
            var vehicles = Object.FindObjectsOfType<VehicleController>();
            if (vehicles == null)
                return false;

            var random = new System.Random(StartOfRound.Instance.randomMapSeed + 91);
            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (
                    !item.itemProperties.isScrap
                    || item.isInShipRoom
                    || item.transform.IsChildOf(ship.transform)
                    || vehicles.Any(_car => item.transform.IsChildOf(_car.transform))
                )
                    continue;
                else
                {
                    if (IsBees(item))
                    {
                        outsideShipBees++;
                        outsideShipValueBees += a(true, item, i);
                    }
                    else
                    {
                        outsideShip++;
                        outsideShipValueNoBees += a(true, item, i);
                    }
                }
            }

            return true;

            int a(bool randomize, GrabbableObject item, int i) =>
                randomize
                    ? Mathf.Clamp(
                        random.Next(item.itemProperties.minValue, item.itemProperties.maxValue),
                        item.scrapValue - 6 * i,
                        item.scrapValue + 9 * i
                    )
                    : item.scrapValue;
        }

        private static bool IsBees(GrabbableObject item)
        {
            AutoScan.Logger.LogDebug($">> IsBees({item}) name:{item.name}");
            return item.name == "RedLocustHive(Clone)" || item.name == "KiwiBabyItem(Clone)";
        }

        // ReSharper disable once UnusedMember.Local
        private static void Postfix(ref StartOfRound __instance)
        {
            ReloadConfig();
            Logger.LogDebug(
                $">> AutoAnnouncePatch({__instance}) IsServer:{__instance.IsServer} AutoAnnounce:{AutoAnnounce}"
            );
            if (!AutoAnnounce || (!__instance.IsServer && HostOnly))
                return;
            Instance.StartCoroutine(DelayedAutoAnnounce(__instance));
            return;

            static System.Collections.IEnumerator DelayedAutoAnnounce(StartOfRound instance)
            {
                yield return new UnityEngine.WaitForSeconds(2f);

                Logger.LogDebug($">> AutoAnnouncePatch delayed for {2f}s");
                if (
                    Countitems(
                        out var error,
                        out var outsideShip,
                        out var outsideShipBees,
                        out var outsideShipValueNoBees,
                        out var outsideShipValueBees
                    )
                )
                {
                    Logger.LogInfo(
                        $"{outsideShip} {outsideShipBees} {outsideShipValueNoBees} {outsideShipValueBees}"
                    );
                    if (outsideShip > 0 || outsideShipBees > 0)
                    {
                        var msg = $"<color=#00ff00>Scan: {outsideShip}";
                        if (AnnounceOutsideLoot)
                            msg += $"/{outsideShipBees}";
                        if (AnnounceValue)
                            msg += $" {outsideShipValueNoBees}";
                        if (AnnounceOutsideLoot && AnnounceValue)
                            msg += $"/{outsideShipValueBees}";
                        msg += "</color>";
                        HUDManager.Instance.AddTextMessageServerRpc(msg);
                    }
                }
                else
                {
                    Logger.LogError($"Error while counting items: {error}");
                    HUDManager.Instance.AddTextMessageServerRpc(
                        $"<color=#ff0000>Error while counting items: {error}</color>"
                    );
                }
            }
        }
    }
}
