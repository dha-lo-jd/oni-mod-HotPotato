using Newtonsoft.Json;
using PeterHan.PLib.Options;

namespace HotPotato.Options;

// The settings, edited in game from the tool menu panel (Ui/SettingsPanel). There is no mod options screen: this class
// is only the saved state (config.json in the mod folder, read and written through PLib).
[JsonObject(MemberSerialization.OptIn)]
public sealed class ModOptions : SingletonOptions<ModOptions>
{
    public const float DefaultAboveCelsius = 100f;
    public const float DefaultBelowCelsius = 0f;

    // Two independent watches, each with its own threshold. An item warns when it is strictly above the "above"
    // threshold or strictly below the "below" one, like the vanilla temperature sensors.
    [JsonProperty]
    public bool WarnAbove { get; set; } = true;

    [JsonProperty]
    public float AboveCelsius { get; set; } = DefaultAboveCelsius;

    [JsonProperty]
    public bool WarnBelow { get; set; } = false;

    [JsonProperty]
    public float BelowCelsius { get; set; } = DefaultBelowCelsius;

    // Shared by both watches.
    [JsonProperty]
    public bool PauseGame { get; set; } = true;

    [JsonProperty]
    public bool ShowNotification { get; set; } = true;

    [JsonProperty]
    public bool IncludeAutoSweepers { get; set; } = true;
}
