using System.Linq;
using HarmonyLib;
using HotPotato.Tools;
using UnityEngine;

namespace HotPotato.Patches;

// Registers the settings tool like a vanilla one: a tool object on the PlayerController and a button in the basic tool menu.
[HarmonyPatch(typeof(PlayerController), "OnPrefabInit")]
internal static class PlayerControllerOnPrefabInitPatch
{
    public static void Postfix(PlayerController __instance)
    {
        if (__instance.tools.OfType<SettingsTool>().Any())
        {
            return;
        }

        // Created inactive and activated once so Awake / OnSpawn run, as for the prefab tools.
        var go = new GameObject(SettingsTool.ToolName);
        go.SetActive(false);
        go.transform.SetParent(__instance.gameObject.transform, false);
        var tool = go.AddComponent<SettingsTool>();
        go.SetActive(true);
        go.SetActive(false);
        __instance.tools = __instance.tools.Concat(new InterfaceTool[] { tool }).ToArray();
    }
}

[HarmonyPatch(typeof(ToolMenu), "CreateBasicTools")]
internal static class ToolMenuCreateBasicToolsPatch
{
    public static void Postfix(ToolMenu __instance)
    {
        // CreateBasicTools can run more than once on the same menu; add the button only once.
        if (__instance.basicTools.Any(c => c.tools.Count > 0 && c.tools[0].toolName == SettingsTool.ToolName))
        {
            return;
        }

        __instance.basicTools.Add(ToolMenu.CreateToolCollection(STRINGS.TOOL.NAME, "overlay_temperature", Action.NumActions,
            SettingsTool.ToolName, STRINGS.TOOL.TOOLTIP, largeIcon: false));
    }
}

// The button has no hotkey. Vanilla compares, indexes and formats hotkeys by action, and breaks on the unbound
// Action.NumActions: an assert in CompareActionKeyCodes, an overflow in IsAction (ToolMenu.OnKeyDown with debug mode
// on) and a missing key binding lookup in GetHotkeyString. Other mods with hotkey-less tools patch the same way.
[HarmonyPatch(typeof(GameInputMapping), "CompareActionKeyCodes")]
internal static class CompareActionKeyCodesPatch
{
    public static bool Prefix(Action a, Action b, ref bool __result)
    {
        if (a == Action.NumActions || b == Action.NumActions)
        {
            __result = false;
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(KButtonEvent), nameof(KButtonEvent.IsAction))]
internal static class IsActionPatch
{
    public static bool Prefix(Action action, ref bool __result)
    {
        if (action == Action.NumActions)
        {
            __result = false;
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(GameUtil), nameof(GameUtil.GetHotkeyString))]
internal static class GetHotkeyStringPatch
{
    public static bool Prefix(Action action, ref string __result)
    {
        if (action == Action.NumActions)
        {
            __result = "";
            return false;
        }

        return true;
    }
}
