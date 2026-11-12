using System.IO;
using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;
using UnityEngine;

namespace HotPotato;

public sealed class HotPotatoMod : UserMod2
{
    internal static string modPath;

    public override void OnLoad(Harmony harmony)
    {
        base.OnLoad(harmony);
        modPath = path;
        PUtil.InitLibrary(false);
    }

    // Registers the strings and overloads them with translations/<language code>.po of the selected language.
    [HarmonyPatch(typeof(Localization), nameof(Localization.Initialize))]
    public static class LocalizationInitializePatch
    {
        public static void Postfix()
        {
            Localization.RegisterForTranslation(typeof(STRINGS));

            var code = Localization.GetLocale()?.Code;
            if (!string.IsNullOrEmpty(code) && modPath != null)
            {
                var file = Path.Combine(modPath, "translations", code + ".po");
                if (File.Exists(file))
                {
                    Localization.OverloadStrings(Localization.LoadStringsFile(file, false));
                    Debug.Log("[HotPotato] Loaded translation " + code);
                }
            }

            LocString.CreateLocStringKeys(typeof(STRINGS), null);
        }
    }
}
