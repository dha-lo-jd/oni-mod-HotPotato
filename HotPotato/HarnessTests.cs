#if DEBUG
// In-game tests run by Gravitas.TestHarness (python Gravitas.TestProbe/probe.py run --project <this repo> --tests).
// The harness matches these types by simple name, so this mod does not reference the harness assembly.
using System;
using System.Linq;
using HarmonyLib;
using HotPotato.Core;
using HotPotato.Options;
using HotPotato.Patches;
using HotPotato.Tools;
using HotPotato.Ui;
using Klei.AI;
using PeterHan.PLib.Options;
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
        // A fresh instance: the defaults, not whatever is saved in config.json.
        var options = new ModOptions();
        var above = ModOptions.DefaultAboveCelsius + 273.15f;
        var below = ModOptions.DefaultBelowCelsius + 273.15f;

        // Defaults: only the "above" watch is on.
        Assert(!TemperatureRule.IsOutOfRange(above, options), "above: equal must not warn");
        Assert(TemperatureRule.IsOutOfRange(above + 0.1f, options), "above: hotter must warn");
        Assert(!TemperatureRule.IsOutOfRange(95f + 273.15f, options), "above: cooked food (95 C) must not warn by default");
        Assert(!TemperatureRule.IsOutOfRange(below - 50f, options), "below: must not warn while that watch is off");

        // Both watches on, with their own thresholds.
        options.WarnBelow = true;
        options.BelowCelsius = -10f;
        Assert(!TemperatureRule.IsOutOfRange(-10f + 273.15f, options), "below: equal must not warn");
        Assert(TemperatureRule.IsOutOfRange(-10.1f + 273.15f, options), "below: colder must warn");
        Assert(!TemperatureRule.IsOutOfRange(20f + 273.15f, options), "room temperature must not warn");
        Assert(TemperatureRule.IsOutOfRange(above + 0.1f, options), "above: still warns with both on");

        // The "above" watch off: only the cold side is left.
        options.WarnAbove = false;
        Assert(!TemperatureRule.IsOutOfRange(above + 100f, options), "above: must not warn while that watch is off");
        Assert(TemperatureRule.IsOutOfRange(-20f + 273.15f, options), "below: still warns with only that watch on");
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
        var saved = Capture(options);
        Configure(options, above: true, below: false, pause: false, notify: true);

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
            Restore(options, saved);
        }
    }

    // The two watches are independent: a cold item warns only while "below" is on, a hot one only while "above" is on.
    [HarnessTest(Name = "hotpotato.pickup_watches", Tags = "hotpotato")]
    public static void PickupWatches()
    {
        var options = ModOptions.Instance;
        var saved = Capture(options);
        var coldKelvin = -50f + 273.15f;
        var worker = TestWorker.Create("HotPotato test worker");
        var spawned = new System.Collections.Generic.List<GameObject>();
        try
        {
            Configure(options, above: false, below: true, pause: false, notify: false);
            options.BelowCelsius = 0f;
            Assert(Raises(worker, spawned, coldKelvin), "cold item did not warn with the below watch on");
            Assert(!Raises(worker, spawned, HotKelvin), "hot item warned with the above watch off");

            options.WarnBelow = false;
            Assert(!Raises(worker, spawned, coldKelvin), "cold item warned with the below watch off");

            options.WarnAbove = true;
            Assert(Raises(worker, spawned, HotKelvin), "hot item did not warn with the above watch on");
            Assert(!Raises(worker, spawned, coldKelvin), "cold item warned with only the above watch on");
        }
        finally
        {
            Cleanup(worker, spawned.ToArray());
            Restore(options, saved);
        }
    }

    // True when picking up an item of this temperature raised an alert.
    private static bool Raises(TestWorker worker, System.Collections.Generic.List<GameObject> spawned, float kelvin)
    {
        var before = PickupAlert.RaisedCount;
        var item = SpawnIgneous(kelvin);
        spawned.Add(item);
        Pickup(worker, item);
        return PickupAlert.RaisedCount > before;
    }

    private static (bool, float, bool, float, bool, bool, bool) Capture(ModOptions o) =>
        (o.WarnAbove, o.AboveCelsius, o.WarnBelow, o.BelowCelsius, o.PauseGame, o.ShowNotification, o.IncludeAutoSweepers);

    private static void Restore(ModOptions o, (bool, float, bool, float, bool, bool, bool) s) =>
        (o.WarnAbove, o.AboveCelsius, o.WarnBelow, o.BelowCelsius, o.PauseGame, o.ShowNotification, o.IncludeAutoSweepers) = s;

    // Default thresholds, auto-sweepers included.
    private static void Configure(ModOptions o, bool above, bool below, bool pause, bool notify)
    {
        o.WarnAbove = above;
        o.AboveCelsius = ModOptions.DefaultAboveCelsius;
        o.WarnBelow = below;
        o.BelowCelsius = ModOptions.DefaultBelowCelsius;
        o.PauseGame = pause;
        o.ShowNotification = notify;
        o.IncludeAutoSweepers = true;
    }

    // Pausing must happen once: pausing an already paused game would need two presses to resume.
    [HarnessTest(Name = "hotpotato.pause_once", Tags = "hotpotato")]
    public static void PauseOnce()
    {
        var options = ModOptions.Instance;
        var saved = Capture(options);
        Configure(options, above: true, below: false, pause: true, notify: false);

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
            Restore(options, saved);
        }
    }

    // The tool menu button opens the settings panel; every control changes its option at once.
    [HarnessTest(Name = "hotpotato.settings_panel", Tags = "hotpotato")]
    public static void SettingsPanelEdits()
    {
        var controller = PlayerController.Instance;
        var tool = controller.tools.OfType<SettingsTool>().FirstOrDefault();
        Assert(tool != null, "the settings tool is not registered on the PlayerController");
        Assert(ToolMenu.Instance.basicTools.Any(c => c.tools.Count > 0 && c.tools[0].toolName == SettingsTool.ToolName),
            "the tool menu has no Hot Potato button");

        var options = ModOptions.Instance;
        var saved = Capture(options);
        Configure(options, above: true, below: false, pause: true, notify: true);
        try
        {
            controller.ActivateTool(tool);
            var panel = SettingsPanel.Root;
            Assert(panel != null && panel.activeInHierarchy, "the settings panel is not shown");

            // Hierarchy order: above, below, pause, notification, auto-sweepers.
            var boxes = panel.GetComponentsInChildren<MultiToggle>().ToArray();
            Assert(boxes.Length == 5, "expected 5 checkboxes in the panel, found " + boxes.Length);
            boxes[0].onClick();
            Assert(!options.WarnAbove, "checkbox 1 did not turn the above watch off");
            boxes[0].onClick();
            Assert(options.WarnAbove, "checkbox 1 did not turn the above watch back on");
            boxes[1].onClick();
            Assert(options.WarnBelow, "checkbox 2 did not turn the below watch on");
            boxes[2].onClick();
            Assert(!options.PauseGame, "checkbox 3 did not turn pausing off");
            boxes[3].onClick();
            Assert(!options.ShowNotification, "checkbox 4 did not turn the notification off");
            boxes[4].onClick();
            Assert(!options.IncludeAutoSweepers, "checkbox 5 did not turn auto-sweepers off");

            // The two thresholds are separate fields.
            var fields = panel.GetComponentsInChildren<TMPro.TMP_InputField>().ToArray();
            Assert(fields.Length == 2, "expected 2 temperature fields in the panel, found " + fields.Length);
            Commit(fields[0], "123.5");
            Assert(Mathf.Abs(options.AboveCelsius - 123.5f) < 0.001f && options.BelowCelsius == ModOptions.DefaultBelowCelsius,
                "the first field did not set only the above threshold");
            Commit(fields[1], "-20");
            Assert(Mathf.Abs(options.BelowCelsius + 20f) < 0.001f && Mathf.Abs(options.AboveCelsius - 123.5f) < 0.001f,
                "the second field did not set only the below threshold");

            // Unusable input keeps the old value and puts it back in the field; huge values are clamped.
            Commit(fields[0], "abc");
            Assert(Mathf.Abs(options.AboveCelsius - 123.5f) < 0.001f && fields[0].text == "123.5",
                "invalid input changed the threshold or left the field showing it (" + fields[0].text + ")");
            Commit(fields[0], "99999");
            Assert(options.AboveCelsius == 10000f, "a huge value was not clamped: " + options.AboveCelsius);

            // Reopening shows the current options again.
            controller.ActivateTool(SelectTool.Instance);
            Assert(!panel.activeInHierarchy, "the panel stayed visible after leaving the tool");
            options.WarnBelow = false;
            options.BelowCelsius = 5f;
            controller.ActivateTool(tool);
            Assert(fields[1].text == "5", "reopened panel shows " + fields[1].text + " instead of the current below threshold");
        }
        finally
        {
            controller.ActivateTool(SelectTool.Instance);
            Restore(options, saved);
            // The controls saved the options on every change; leave the file as it was.
            POptions.WriteSettings(options);
        }
    }

    // Typing a value and leaving the field, as the player does.
    private static void Commit(TMPro.TMP_InputField field, string text)
    {
        field.text = text;
        field.onEndEdit.Invoke(text);
    }

    // Not a test: lets the harness (/invoke) open the panel so a screenshot can show it.
    public static void ShowSettingsPanel()
    {
        var tool = PlayerController.Instance.tools.OfType<SettingsTool>().First();
        PlayerController.Instance.ActivateTool(tool);
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
