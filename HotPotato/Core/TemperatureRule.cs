using HotPotato.Options;

namespace HotPotato.Core;

internal static class TemperatureRule
{
    private const float KelvinOffset = 273.15f;

    // Strictly above or strictly below the threshold, like the vanilla temperature sensors.
    public static bool IsOutOfRange(float temperatureKelvin, ThresholdMode mode, float thresholdCelsius)
    {
        var thresholdKelvin = thresholdCelsius + KelvinOffset;
        return mode == ThresholdMode.Above
            ? temperatureKelvin > thresholdKelvin
            : temperatureKelvin < thresholdKelvin;
    }
}
