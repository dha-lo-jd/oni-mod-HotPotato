# Hot Potato

An Oxygen Not Included mod that **warns instead of forbidding**: when a Duplicant or an Auto-Sweeper picks up an item
whose temperature is above (or below) a threshold, the game pauses and a notification tells you who picked up what.

Work in progress.

## Planned behavior

- Trigger: the moment a pickup completes (`Pickupable.OnCompleteWork`), for Duplicants and Auto-Sweepers.
- Check: item temperature against one threshold, "above" or "below", like the vanilla temperature sensors.
  Default: above 100 °C (cooking stations output food at 95 °C).
- Reaction: pause the game and/or show a notification ("Handle with care!"); clicking it focuses the carrier.
  Pausing and notifying can be switched on and off separately.

## Local build setup

1. Copy `HotPotato/Local.Build.props.example` to `HotPotato/Local.Build.props` (git-ignored).
2. Set `GameManagedPath` (`OxygenNotIncluded_Data\Managed`) and `ModInstallPath` (e.g. `...\mods\local\HotPotato`).
   Alternatively set the environment variables `ONI_GAME_MANAGED_PATH` / `ONI_MOD_INSTALL_PATH`.
3. `dotnet build` merges PLib into the DLL (ILRepack), then copies the DLL, pdb, `mod.yaml`, `mod_info.yaml` and
   `translations/*.po` to `ModInstallPath`.
4. `dotnet build -c Release` produces a distributable folder in `release/HotPotato` (git-ignored).

## License

MIT
