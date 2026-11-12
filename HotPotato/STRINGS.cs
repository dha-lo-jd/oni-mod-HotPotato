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

    // The tool menu button. Its name is also the title of the settings panel.
    public static class TOOL
    {
        public static LocString NAME = "Hot Potato Settings";
        public static LocString TOOLTIP = "Change the pickup temperature warnings.";
    }

    public static class PANEL
    {
        public static LocString UNIT = "°C";
        public static LocString THRESHOLD_TOOLTIP = "Temperature in °C. Press Enter or click elsewhere to apply.";

        public static class WARN_ABOVE
        {
            public static LocString NAME = "Warn above";
            public static LocString TOOLTIP = "Warn when a picked-up item is hotter than this temperature. Cooking stations output food at 95 °C and the Kiln at 80 °C, so a value below those warns about every meal or brick.";
        }

        public static class WARN_BELOW
        {
            public static LocString NAME = "Warn below";
            public static LocString TOOLTIP = "Warn when a picked-up item is colder than this temperature.";
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
