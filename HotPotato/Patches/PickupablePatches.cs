using System;
using HarmonyLib;
using HotPotato.Core;
using HotPotato.Options;
using UnityEngine;

namespace HotPotato.Patches;

// Every worker pickup (Duplicants, Auto-Sweepers, robots) ends in Pickupable.OnCompleteWork,
// which takes the requested amount and stores it in the worker's storage.
[HarmonyPatch(typeof(Pickupable), "OnCompleteWork")]
internal static class PickupableOnCompleteWorkPatch
{
    private static bool loggedError;

    // The temperature is read before Take(), which may merge or destroy this object.
    public static void Prefix(Pickupable __instance, WorkerBase worker, out PickupRecord __state)
    {
        __state = null;
        try
        {
            __state = Inspect(__instance, worker);
        }
        catch (Exception e)
        {
            LogOnce(e);
        }
    }

    public static void Postfix(PickupRecord __state)
    {
        if (__state == null)
        {
            return;
        }
        try
        {
            PickupAlert.Raise(__state);
        }
        catch (Exception e)
        {
            LogOnce(e);
        }
    }

    private static PickupRecord Inspect(Pickupable pickupable, WorkerBase worker)
    {
        if (pickupable == null || worker == null)
        {
            return null;
        }

        var options = ModOptions.Instance;
        if (!options.IncludeAutoSweepers && worker.GetComponent<SolidTransferArm>() != null)
        {
            return null;
        }

        var element = pickupable.GetComponent<PrimaryElement>();
        if (element == null || !TemperatureRule.IsOutOfRange(element.Temperature, options.Mode, options.ThresholdCelsius))
        {
            return null;
        }

        return new PickupRecord
        {
            Carrier = worker,
            CarrierName = worker.GetProperName(),
            ItemName = pickupable.GetProperName(),
            TemperatureKelvin = element.Temperature,
        };
    }

    // Never break a pickup because of this mod; report the first failure only.
    private static void LogOnce(Exception e)
    {
        if (loggedError)
        {
            return;
        }
        loggedError = true;
        Debug.LogWarning("[HotPotato] Pickup check failed: " + e);
    }
}
