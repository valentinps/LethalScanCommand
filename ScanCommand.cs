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
        LethalScanCommand.Logger.LogDebug($">> GetString(compact:{compact})");
        if (
            !Countitems(
                out error,
                out var inShip,
                out var inShipBees,
                out var inShipValue,
                out var onShip,
                out var onShipBees,
                out var onShipValue,
                out var onCruiser,
                out var onCruiserBees,
                out var onCruiserValue,
                out var outsideShip,
                out var outsideShipBees,
                out var outsideShipValueNoBees,
                out var outsideShipValueBees,
                out var carValueApproximated
            )
        )
        {
            LethalScanCommand.Logger.LogDebug($"<< CountItems returned false error:{error}");
            return false;
        }
        if (compact)
        {
            List<string> lines = [];
            if (inShip > 0 || inShipBees > 0)
                lines.Add(
                    $"In ship: {inShip}/{inShipBees} items worth {c(LethalScanCommand.ApproximateValueInShip, inShipValue)}"
                );
            if (onShip > 0 || onShipBees > 0)
                lines.Add(
                    $"On ship: {onShip}/{onShipBees} items worth {c(LethalScanCommand.ApproximateValueOnShip, onShipValue)}"
                );
            if (onCruiser > 0 || onCruiserBees > 0)
                lines.Add(
                    $"On cruiser: {onCruiser}/{onCruiserBees} items worth {c(carValueApproximated, onCruiserValue)}"
                );
            if (!StartOfRound.Instance.inShipPhase)
                lines.Add(
                    $"Outside ship: {outsideShip}/{outsideShipBees} items{(outsideShip > 0 || outsideShipBees > 0 ? $" worth {c(LethalScanCommand.ApproximateValueOutsideShip, outsideShipValueNoBees)}" : string.Empty)}"
                );
            LethalScanCommand.Logger.LogDebug($"   lines:{d(lines)}");
            text = lines.Count == 0 ? "No items" : lines.Join(null, "\n");
        }
        else
        {
            List<string> strings = [];

            int first = 0;
            if (inShip > 0 || inShipBees > 0)
                strings.Add(
                    $"{a(inShip, inShipBees, ref first, strings.Count == 0)} worth {c(LethalScanCommand.ApproximateValueInShip, inShipValue)} in the ship"
                );
            if (onShip > 0 || onShipBees > 0)
                strings.Add(
                    $"{a(onShip, onShipBees, ref first, strings.Count == 0)} worth {c(LethalScanCommand.ApproximateValueOnShip, onShipValue)} on the ship"
                );
            if (onCruiser > 0 || onCruiserBees > 0)
                strings.Add(
                    $"{a(onCruiser, onCruiserBees, ref first, strings.Count == 0)} worth {c(carValueApproximated, onCruiserValue)} on the cruiser"
                );
            if (!StartOfRound.Instance.inShipPhase)
                strings.Add(
                    $"{a(outsideShip, outsideShipBees, ref first, strings.Count == 0)}{(outsideShip > 0 ? $" worth {c(LethalScanCommand.ApproximateValueOutsideShip, outsideShipValueNoBees)}" : string.Empty)} outside the ship"
                );

            LethalScanCommand.Logger.LogDebug($"   strings:{d(strings)}");
            text = strings.Count switch
            {
                0 => "There are no items",
                1 => $"There {f(first)} " + strings[0],
                _ => $"There {f(first)} "
                    + strings.GetRange(0, strings.Count - 1).Join()
                    + " and "
                    + strings.Last(),
            };
        }

        return true;

        string a(int items, int bees, ref int first, bool isFirst)
        {
            if (isFirst)
                first = items > 0 ? items : bees;
            return items > 0 ? $"{items} item{e(items)}{b(bees)}" : $"{bees} beehive{e(bees)}";
        }

        string b(int bees) => bees > 0 ? $" and {bees} beehive{e(bees)}" : string.Empty;
        string c(bool approx, int val) => $"{(approx ? "~" : string.Empty)}{val}$";
        string d(List<string> list) =>
            $"List<string>[{list.Count}]: [{list.Join(i => $"\"{i}\"")}]";

        string e(int val) => val == 1 ? string.Empty : "s";
        string f(int first) => first == 1 ? "is" : "are";
    }

    public static bool Countitems(
        out string? error,
        out int inShip,
        out int inShipBees,
        out int inShipValue,
        out int onShip,
        out int onShipBees,
        out int onShipValue,
        out int onCruiser,
        out int onCruiserBees,
        out int onCruiserValue,
        out int outsideShip,
        out int outsideShipBees,
        out int outsideShipValueNoBees,
        out int outsideShipValueBees,
        out bool carValueApproximated
    )
    {
        inShip = 0;
        inShipBees = 0;
        inShipValue = 0;
        onShip = 0;
        onShipBees = 0;
        onShipValue = 0;
        onCruiser = 0;
        onCruiserBees = 0;
        onCruiserValue = 0;
        outsideShip = 0;
        outsideShipBees = 0;
        outsideShipValueNoBees = 0;
        outsideShipValueBees = 0;
        carValueApproximated = false;

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

        carValueApproximated = b(
            LethalScanCommand.ApproximateValueOnCruiser,
            vehicles.Any(i => i.magnetedToShip)
        );

        var random = new System.Random(StartOfRound.Instance.randomMapSeed + 91);
        for (var i = 0; i < items.Length; i++)
        {
            var item = items[i];
            if (!item.itemProperties.isScrap)
                continue;
            if (item.isInShipRoom)
            {
                if (IsBees(item))
                    inShipBees++;
                else
                    inShip++;
                inShipValue += a(LethalScanCommand.ApproximateValueInShip, item, i);
            }
            else if (item.transform.IsChildOf(ship.transform))
            {
                if (IsBees(item))
                    onShipBees++;
                else
                    onShip++;
                onShipValue += a(LethalScanCommand.ApproximateValueOnShip, item, i);
            }
            else if (vehicles.Any(_car => item.transform.IsChildOf(_car.transform)))
            {
                if (IsBees(item))
                    onCruiserBees++;
                else
                    onCruiser++;
                onCruiserValue += a(carValueApproximated, item, i);
            }
            else
            {
                if (IsBees(item)) {
                    outsideShipBees++;
                    outsideShipValueBees += a(LethalScanCommand.ApproximateValueOutsideShip, item, i);
                }
                else
                {
                    outsideShip++;
                    outsideShipValueNoBees += a(LethalScanCommand.ApproximateValueOutsideShip, item, i);
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
        bool b(LethalScanCommand.ApproximateValueOnCruiserOptions value, bool carMagnetedToShip) =>
            value switch
            {
                LethalScanCommand.ApproximateValueOnCruiserOptions.Always => true,
                LethalScanCommand.ApproximateValueOnCruiserOptions.Never => false,
                _ => !carMagnetedToShip,
            };
    }

    private static bool IsBees(GrabbableObject item)
    {
        LethalScanCommand.Logger.LogDebug($">> IsBees({item}) name:{item.name}");
        return item.name == "RedLocustHive(Clone)" || item.name == "KiwiBabyItem(Clone)";
    }
}
