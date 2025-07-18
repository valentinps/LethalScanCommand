using System.Collections.Generic;
using System.Linq;
using ChatCommandAPI;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace LethalScanCommand;

public class ScanCommand : ServerCommand
{
    public override string[] Commands => ["scan", Name];
    public override string Description => "Scans for items in- or outside the ship";

    public override bool Invoke(
        ref PlayerControllerB? caller,
        string[] args,
        Dictionary<string, string> kwargs,
        out string? error
    )
    {
        error = "caller is null";
        if (
            caller == null
            || !GetString(out var text, out error, LethalScanCommand.CompactResponse)
        )
            return false;
        ChatCommandAPI.ChatCommandAPI.Print(caller, text);
        return true;
    }

    public static bool GetString(out string text, out string? error, bool compact = true)
    {
        text = string.Empty;
        if (
            !Countitems(
                out error,
                out var inShip,
                out var inShipValue,
                out var onShip,
                out var onShipValue,
                out var onCruiser,
                out var onCruiserValue,
                out var outsideShip,
                out var outsideShipValue
            )
        )
            return false;
        if (compact)
        {
            List<string> lines = [];
            if (inShip > 0)
                lines.Add($"In ship: {inShip} items worth {inShipValue}$");
            if (onShip > 0)
                lines.Add($"On ship: {onShip} items worth {onShipValue}$");
            if (onCruiser > 0)
                lines.Add($"On cruiser: {onCruiser} items worth {onCruiserValue}$");

            lines.Add(
                $"Outside ship: {outsideShip} items{(outsideShip > 0 ? $" worth {outsideShipValue}$" : string.Empty)}"
            );
            text = lines.Join(null, "\n");
        }
        else
        {
            if (inShip > 0)
            {
                text = $"There are {inShip} items worth {inShipValue}$ in the ship";
                if (onShip > 0)
                    text += $", {onShip} items worth {onShipValue}$ on the ship";
                if (onCruiser > 0)
                    text += $", {onCruiser} items worth {onCruiserValue}$ on the cruiser";
                text +=
                    $" and {outsideShip} items{(outsideShip > 0 ? $" worth {outsideShipValue}$" : string.Empty)} outside the ship";
            }
            else if (onShip > 0)
            {
                text = $"There are {onShip} items worth {onShipValue}$ on the ship";
                if (onCruiser > 0)
                    text += $", {onCruiser} items worth {onCruiserValue}$ on the cruiser";
                text +=
                    $" and {outsideShip} items{(outsideShip > 0 ? $" worth {outsideShipValue}$" : string.Empty)} outside the ship";
            }
            else if (onCruiser > 0)
                text =
                    $"There are {onCruiser} items worth {onCruiserValue}$ on the cruiser"
                    + $" and {outsideShip} items{(outsideShip > 0 ? $" worth {outsideShipValue}$" : string.Empty)} outside the ship";
            else
                text =
                    $"There are {outsideShip} items{(outsideShip > 0 ? $" worth {outsideShipValue}$" : string.Empty)} outside the ship";
        }

        return true;
    }

    public static bool Countitems(
        out string? error,
        out int inShip,
        out int inShipValue,
        out int onShip,
        out int onShipValue,
        out int onCruiser,
        out int onCruiserValue,
        out int outsideShip,
        out int outsideShipValue
    )
    {
        inShip = 0;
        inShipValue = 0;
        onShip = 0;
        onShipValue = 0;
        onCruiser = 0;
        onCruiserValue = 0;
        outsideShip = 0;
        outsideShipValue = 0;

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
            if (!item.itemProperties.isScrap)
                continue;
            if (item.isInShipRoom)
            {
                inShip++;
                inShipValue += val(random, item, i);
            }
            else if (item.transform.IsChildOf(ship.transform))
            {
                onShip++;
                onShipValue += val(random, item, i);
            }
            else if (vehicles.Any(car => item.transform.IsChildOf(car.transform)))
            {
                onCruiser++;
                onCruiserValue += val(random, item, i);
            }
            else
            {
                outsideShip++;
                outsideShipValue += val(random, item, i);
            }
        }

        return true;
    }

    private static int val(System.Random random, GrabbableObject item, int i) =>
        LethalScanCommand.ApproximateValue
            ? Mathf.Clamp(
                random.Next(item.itemProperties.minValue, item.itemProperties.maxValue),
                item.scrapValue - 6 * i,
                item.scrapValue + 9 * i
            )
            : item.scrapValue;
}
