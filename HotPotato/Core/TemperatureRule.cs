using HotPotato.Options;

namespace HotPotato.Core;

internal static class TemperatureRule
{
    private const float KelvinOffset = 273.15f;

    // Strictly above or strictly below the threshold, like the vanilla temperature sensors.
    public static bool IsAbove(float temperatureKelvin, float thresholdCelsius) =>
        temperatureKelvin > thresholdCelsius + KelvinOffset;

    public static bool IsBelow(float temperatureKelvin, float thresholdCelsius) =>
        temperatureKelvin < thresholdCelsius + KelvinOffset;

    // Each watch can be switched off on its own.
    public static bool IsOutOfRange(float temperatureKelvin, ModOptions options) =>
        (options.WarnAbove && IsAbove(temperatureKelvin, options.AboveCelsius))
        || (options.WarnBelow && IsBelow(temperatureKelvin, options.BelowCelsius));
}
