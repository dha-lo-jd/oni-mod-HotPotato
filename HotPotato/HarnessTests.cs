#if DEBUG
// In-game tests run by Gravitas.TestHarness (python Gravitas.TestProbe/probe.py run --project <this repo> --tests).
// The harness matches these types by simple name, so this mod does not reference the harness assembly.
using System;
using System.Linq;
using HarmonyLib;
using HotPotato.Core;
using HotPotato.Options;
using HotPotato.Patches;
using Klei.AI;
using UnityEngine;

namespace HotPotato;

[AttributeUsage(AttributeTargets.Method)]
internal sealed class HarnessTestAttribute : System.Attribute
{
    public string Name { get; set; }
    public string Tags { get; set; }
    public bool RequiresGame { get; set; } = true;
}

internal sealed class HarnessAssertionException : Exception
{
    public HarnessAssertionException(string message) : base(message) { }
}

internal static class HarnessTests
{
    private const float HotKelvin = 400f + 273.15f;
    private const float RoomKelvin = 20f + 273.15f;

    [HarnessTest(Name = "hotpotato.rule_thresholds", Tags = "hotpotato", RequiresGame = false)]
    public static void RuleThresholds()
    {
        var threshold = ModOptions.DefaultThresholdCelsius;
        var atThreshold = threshold + 273.15f;
        Assert(!TemperatureRule.IsOutOfRange(atThreshold, ThresholdMode.Above, threshold), "above: equal must not warn");
        Assert(TemperatureRule.IsOutOfRange(atThreshold + 0.1f, ThresholdMode.Above, threshold), "above: hotter must warn");
        Assert(!TemperatureRule.IsOutOfRange(95f + 273.15f, ThresholdMode.Above, threshold), "above: cooked food (95 C) must not warn by default");
        Assert(!TemperatureRule.IsOutOfRange(atThreshold, ThresholdMode.Below, threshold), "below: equal must not warn");
        Assert(TemperatureRule.IsOutOfRange(atThreshold - 0.1f, ThresholdMode.Below, threshold), "below: colder must warn");
    }

    [HarnessTest(Name = "hotpotato.patch_applied", Tags = "hotpotato", RequiresGame = false)]
    public static void PatchApplied()
    {
        var info = Harmony.GetPatchInfo(AccessTools.Method(typeof(Pickupable), "OnCompleteWork"));
        Assert(info != null, "Pickupable.OnCompleteWork is not patched");
        Assert(info.Prefixes.Any(p => p.PatchMethod.DeclaringType == typeof(PickupableOnCompleteWorkPatch)), "prefix missing");
        Assert(info.Postfixes.Any(p => p.PatchMethod.DeclaringType == typeof(PickupableOnCompleteWorkPatch)), "postfix missing");
    }

    // A real pickup through Pickupable.OnCompleteWork with a minimal worker: hot item warns, room temperature does not.
    [HarnessTest(Name = "hotpotato.pickup_warns", Tags = "hotpotato")]
    public static void PickupWarns()
    {
        var options = ModOptions.Instance;
        var saved = (options.Mode, options.ThresholdCelsius, options.PauseGame, options.ShowNotification);
        options.Mode = ThresholdMode.Above;
        options.ThresholdCelsius = ModOptions.DefaultThresholdCelsius;
        options.PauseGame = false;
        options.ShowNotification = true;

        var worker = TestWorker.Create("HotPotato test worker");
        GameObject hot = null, cool = null;
        try
        {
            var before = PickupAlert.RaisedCount;
            cool = SpawnIgneous(RoomKelvin);
            Pickup(worker, cool);
            Assert(PickupAlert.RaisedCount == before, "room-temperature item raised an alert");

            hot = SpawnIgneous(HotKelvin);
            Pickup(worker, hot);
            Assert(PickupAlert.RaisedCount == before + 1, "hot item did not raise an alert");
            Assert(Mathf.Abs(PickupAlert.LastRecord.TemperatureKelvin - HotKelvin) < 0.5f,
                "recorded temperature " + PickupAlert.LastRecord.TemperatureKelvin + " K");
            Assert(PickupAlert.LastNotification != null && PickupAlert.LastNotification.Notifier != null,
                "notification was not added");
            // Take() may hand over a split-off chunk instead of the spawned object, so check by element and mass.
            var carried = worker.GetComponent<Storage>().GetMassAvailable(SimHashes.IgneousRock);
            Assert(carried >= 99f, "the worker carries " + carried + " kg of igneous rock, expected 100 kg");
        }
        finally
        {
            RemoveLastNotification();
            Cleanup(worker, hot, cool);
            (options.Mode, options.ThresholdCelsius, options.PauseGame, options.ShowNotification) = saved;
        }
    }

    // Pausing must happen once: pausing an already paused game would need two presses to resume.
    [HarnessTest(Name = "hotpotato.pause_once", Tags = "hotpotato")]
    public static void PauseOnce()
    {
        var options = ModOptions.Instance;
        var saved = (options.Mode, options.ThresholdCelsius, options.PauseGame, options.ShowNotification);
        options.Mode = ThresholdMode.Above;
        options.ThresholdCelsius = ModOptions.DefaultThresholdCelsius;
        options.PauseGame = true;
        options.ShowNotification = false;

        var speed = SpeedControlScreen.Instance;
        var pauseCount = Traverse.Create(speed).Field<int>("pauseCount");
        var wasPaused = speed.IsPaused;
        var worker = TestWorker.Create("HotPotato test worker");
        GameObject first = null, second = null;
        try
        {
            if (wasPaused)
            {
                speed.Unpause(false);
            }
            Assert(!speed.IsPaused, "could not unpause before the test (pause count " + pauseCount.Value + ")");

            first = SpawnIgneous(HotKelvin);
            Pickup(worker, first);
            Assert(speed.IsPaused, "the game was not paused");
            var countAfterFirst = pauseCount.Value;

            second = SpawnIgneous(HotKelvin);
            Pickup(worker, second);
            Assert(pauseCount.Value == countAfterFirst, "paused again while already paused (" + countAfterFirst + " -> " + pauseCount.Value + ")");
        }
        finally
        {
            Cleanup(worker, first, second);
            // Leave the pause state as the harness had it.
            if (wasPaused && !speed.IsPaused) speed.Pause(false);
            if (!wasPaused && speed.IsPaused) speed.Unpause(false);
            (options.Mode, options.ThresholdCelsius, options.PauseGame, options.ShowNotification) = saved;
        }
    }

    private static GameObject SpawnIgneous(float kelvin)
    {
        var world = ClusterManager.Instance.activeWorld;
        var cell = Grid.XYToCell(world.WorldOffset.x + world.Width / 2, world.WorldOffset.y + world.Height / 2);
        var substance = ElementLoader.FindElementByHash(SimHashes.IgneousRock).substance;
        return substance.SpawnResource(Grid.CellToPosCCC(cell, Grid.SceneLayer.Ore), 50f, kelvin, byte.MaxValue, 0,
            prevent_merge: true, forceTemperature: true);
    }

    private static void Pickup(TestWorker worker, GameObject item)
    {
        var pickupable = item.GetComponent<Pickupable>();
        worker.Begin(new Pickupable.PickupableStartWorkInfo(pickupable, pickupable.TotalAmount, _ => { }));
        AccessTools.Method(typeof(Pickupable), "OnCompleteWork").Invoke(pickupable, new object[] { worker });
    }

    private static void RemoveLastNotification()
    {
        var notification = PickupAlert.LastNotification;
        if (notification?.Notifier != null)
        {
            notification.Notifier.Remove(notification);
        }
    }

    // Destroys what the worker carries, the worker itself and any spawned items still lying around.
    private static void Cleanup(TestWorker worker, params GameObject[] objects)
    {
        var storage = worker.GetComponent<Storage>();
        foreach (var item in storage.items.ToList())
        {
            if (item != null) Util.KDestroyGameObject(item);
        }
        Util.KDestroyGameObject(worker.gameObject);
        foreach (var go in objects)
        {
            if (go != null) Util.KDestroyGameObject(go);
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new HarnessAssertionException(message);
    }
}

// Just enough of a worker for Pickupable.OnCompleteWork: a storage to put the item in and the start info.
internal sealed class TestWorker : WorkerBase
{
    private StartWorkInfo startWorkInfo;

    public static TestWorker Create(string name)
    {
        var go = new GameObject(name);
        go.SetActive(false);
        go.AddComponent<KPrefabID>();
        go.AddComponent<Storage>();
        var worker = go.AddComponent<TestWorker>();
        go.SetActive(true);
        return worker;
    }

    public void Begin(StartWorkInfo info) => startWorkInfo = info;

    public override bool UsesMultiTool() => false;
    public override bool IsFetchDrone() => false;
    public override KBatchedAnimController GetAnimController() => null;
    public override State GetState() => State.Working;
    public override StartWorkInfo GetStartWorkInfo() => startWorkInfo;
    public override Workable GetWorkable() => startWorkInfo?.workable;
    public override Attributes GetAttributes() => null;
    public override AttributeConverterInstance GetAttributeConverter(string id) => null;
    public override Guid OfferStatusItem(StatusItem item, object data = null) => Guid.Empty;
    public override void RevokeStatusItem(Guid id) { }
    public override void StartWork(StartWorkInfo start_work_info) => startWorkInfo = start_work_info;
    public override void StopWork() => startWorkInfo = null;
    public override bool InstantlyFinish() => false;
    public override WorkResult Work(float dt) => WorkResult.Success;
    public override void SetWorkCompleteData(object data) { }
}
#endif
