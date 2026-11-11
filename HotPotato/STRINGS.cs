namespace HotPotato;

// All user-visible text. Translations are loaded from translations/<language code>.po (see HotPotatoMod).
public static class STRINGS
{
    public static class NOTIFICATION
    {
        public static LocString TITLE = "Handle with care!";
        public static LocString TOOLTIP = "These items were picked up outside the temperature range set in Hot Potato:";

        // {0}: carrier, {1}: item, {2}: temperature
        public static LocString MESSAGE = "{0} picked up {1} ({2})";
    }

    // The tool menu button. The checkbox labels reuse OPTIONS.PAUSE_GAME and OPTIONS.SHOW_NOTIFICATION.
    public static class TOOL
    {
        public static LocString NAME = "Hot Potato Settings";
        public static LocString TOOLTIP = "Turn the pickup temperature warnings on or off. The threshold and mode are in the mod options.";
    }

    public static class OPTIONS
    {
        public static class MODE
        {
            public static LocString NAME = "Warn when temperature is";
            public static LocString TOOLTIP = "Warn when a picked-up item is above or below the threshold, like a Thermo Sensor.";
            public static LocString ABOVE = "Above threshold";
            public static LocString ABOVE_TOOLTIP = "Warn about items hotter than the threshold.";
            public static LocString BELOW = "Below threshold";
            public static LocString BELOW_TOOLTIP = "Warn about items colder than the threshold.";
        }

        public static class THRESHOLD
        {
            public static LocString NAME = "Threshold (°C)";
            public static LocString TOOLTIP = "Temperature that triggers the warning. Cooking stations output food at 95 °C and the Kiln at 80 °C, so an \"above\" threshold below those warns about every meal or brick.";
        }

        public static class PAUSE_GAME
        {
            public static LocString NAME = "Pause the game";
            public static LocString TOOLTIP = "Pause the game when such an item is picked up.";
        }

        public static class SHOW_NOTIFICATION
        {
            public static LocString NAME = "Show a notification";
            public static LocString TOOLTIP = "Show a notification naming the carrier, the item and its temperature. Click it to focus the carrier.";
        }

        public static class INCLUDE_AUTO_SWEEPERS
        {
            public static LocString NAME = "Include Auto-Sweepers";
            public static LocString TOOLTIP = "Also warn when an Auto-Sweeper picks up such an item.";
        }
    }
}
