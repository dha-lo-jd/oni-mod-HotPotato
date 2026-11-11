using Newtonsoft.Json;
using PeterHan.PLib;
using PeterHan.PLib.Options;

namespace HotPotato.Options;

public enum ThresholdMode
{
    [Option("HotPotato.STRINGS.OPTIONS.MODE.ABOVE", "HotPotato.STRINGS.OPTIONS.MODE.ABOVE_TOOLTIP")]
    Above,

    [Option("HotPotato.STRINGS.OPTIONS.MODE.BELOW", "HotPotato.STRINGS.OPTIONS.MODE.BELOW_TOOLTIP")]
    Below,
}

[JsonObject(MemberSerialization.OptIn)]
public sealed class ModOptions : SingletonOptions<ModOptions>
{
    public const float DefaultThresholdCelsius = 100f;

    // Same model as the vanilla temperature sensors: one threshold plus "above" or "below".
    [Option("HotPotato.STRINGS.OPTIONS.MODE.NAME", "HotPotato.STRINGS.OPTIONS.MODE.TOOLTIP")]
    [JsonProperty]
    public ThresholdMode Mode { get; set; } = ThresholdMode.Above;

    [Option("HotPotato.STRINGS.OPTIONS.THRESHOLD.NAME", "HotPotato.STRINGS.OPTIONS.THRESHOLD.TOOLTIP")]
    [Limit(-273.15, 10000)]
    [JsonProperty]
    public float ThresholdCelsius { get; set; } = DefaultThresholdCelsius;

    [Option("HotPotato.STRINGS.OPTIONS.PAUSE_GAME.NAME", "HotPotato.STRINGS.OPTIONS.PAUSE_GAME.TOOLTIP")]
    [JsonProperty]
    public bool PauseGame { get; set; } = true;

    [Option("HotPotato.STRINGS.OPTIONS.SHOW_NOTIFICATION.NAME", "HotPotato.STRINGS.OPTIONS.SHOW_NOTIFICATION.TOOLTIP")]
    [JsonProperty]
    public bool ShowNotification { get; set; } = true;

    [Option("HotPotato.STRINGS.OPTIONS.INCLUDE_AUTO_SWEEPERS.NAME", "HotPotato.STRINGS.OPTIONS.INCLUDE_AUTO_SWEEPERS.TOOLTIP")]
    [JsonProperty]
    public bool IncludeAutoSweepers { get; set; } = true;
}
