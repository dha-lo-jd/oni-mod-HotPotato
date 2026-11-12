# Copilot instructions for HotPotato

ONI (Oxygen Not Included) mod. Structure and build setup intentionally mirror the sibling `StorageFilterIcons` solution.

- `HotPotato/HotPotato.csproj`: `netstandard2.1`, LangVersion 10.0, references game DLLs directly from `$(GameManagedPath)`.
- `HotPotato/Local.Build.props` (git-ignored; template: `Local.Build.props.example`) defines `GameManagedPath` and `ModInstallPath`. Env fallbacks: `ONI_GAME_MANAGED_PATH`, `ONI_MOD_INSTALL_PATH`.
- Debug builds copy the DLL, pdb, `mod.yaml`, `mod_info.yaml`, `translations/*.po` and `ModAssets/*.png` to `$(ModInstallPath)`.
- PLib (NuGet 4.25.0) is embedded into the mod DLL after build via ILRepack (`MergeAssemblies` target); do not ship PLib.dll separately.
- Release builds produce a clean distribution folder in `release/HotPotato` (git-ignored).
- `harness.json` configures in-game tests run by the sibling `GravitasTestHarness`.

## Build

```bash
dotnet build
```

Requires a local ONI install (real game assemblies; there is no stub mode).

## Code layout

- `HotPotatoMod.cs`: `UserMod2` entry point; applies all Harmony patches and loads translations.
- Settings are edited only in game; there is no mod options screen. `Options/ModOptions.cs` is just the saved state (`config.json` in the mod folder, via PLib `SingletonOptions`): two independent watches (`WarnAbove`/`AboveCelsius`, `WarnBelow`/`BelowCelsius`) plus the shared `PauseGame`, `ShowNotification`, `IncludeAutoSweepers`.
- `Tools/SettingsTool.cs` + `Patches/ToolPatches.cs`: a tool menu button (no hotkey) whose tool does nothing on the map and shows `Ui/SettingsPanel.cs`, a PLib UI panel docked like the vanilla tool filter panel. Every control writes `ModOptions` and saves it at once (temperature fields commit on Enter / focus loss).
- File-scoped namespaces, no implicit usings, nullable disabled.
