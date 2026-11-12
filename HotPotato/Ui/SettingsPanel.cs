using System;
using System.Globalization;
using HotPotato.Options;
using PeterHan.PLib.Options;
using PeterHan.PLib.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HotPotato.Ui;

// The settings panel, docked where the vanilla tool filter panel appears (bottom right, above the tool buttons).
// Every control writes the option straight away and saves it, so changes apply to the running game at once.
internal static class SettingsPanel
{
    private const float MinCelsius = -273.15f;
    private const float MaxCelsius = 10000f;

    // The grey of the vanilla tool filter panel.
    private static readonly Color PanelColor = new Color32(73, 73, 73, 255);

    private static GameObject root;
    private static GameObject warnAbove;
    private static GameObject aboveField;
    private static GameObject warnBelow;
    private static GameObject belowField;
    private static GameObject pauseGame;
    private static GameObject showNotification;
    private static GameObject includeSweepers;

    // Read by the in-game tests.
    internal static GameObject Root => root;

    public static void Show()
    {
        // The objects are destroyed with the scene; a destroyed root compares equal to null, so it is rebuilt.
        if (root == null)
        {
            Build();
        }

        Refresh();
        root.SetActive(true);
    }

    public static void Hide()
    {
        if (root != null)
        {
            root.SetActive(false);
        }
    }

    // Shows the current options, which may have changed since the panel was built.
    private static void Refresh()
    {
        var options = ModOptions.Instance;
        SetChecked(warnAbove, options.WarnAbove);
        SetField(aboveField, options.AboveCelsius);
        SetChecked(warnBelow, options.WarnBelow);
        SetField(belowField, options.BelowCelsius);
        SetChecked(pauseGame, options.PauseGame);
        SetChecked(showNotification, options.ShowNotification);
        SetChecked(includeSweepers, options.IncludeAutoSweepers);
    }

    private static void Build()
    {
        var options = ModOptions.Instance;
        var panel = new PPanel("HotPotatoSettings")
        {
            Direction = PanelDirection.Vertical,
            Spacing = 4,
            Margin = new RectOffset(0, 0, 0, 6),
            BackColor = PanelColor,
            Alignment = TextAnchor.UpperLeft,
        };

        var title = new PLabel("Title")
        {
            Text = STRINGS.TOOL.NAME,
            TextStyle = PUITuning.Fonts.UILightStyle,
            Margin = new RectOffset(8, 8, 3, 3),
            FlexSize = new Vector2(1f, 0f),
        }.SetKleiPinkColor();
        panel.AddChild(title);

        panel.AddChild(ThresholdRow(
            "WarnAbove", STRINGS.PANEL.WARN_ABOVE.NAME, STRINGS.PANEL.WARN_ABOVE.TOOLTIP, options.WarnAbove,
            on => ModOptions.Instance.WarnAbove = on, go => warnAbove = go,
            "AboveField", c => ModOptions.Instance.AboveCelsius = c, () => ModOptions.Instance.AboveCelsius, go => aboveField = go));
        panel.AddChild(ThresholdRow(
            "WarnBelow", STRINGS.PANEL.WARN_BELOW.NAME, STRINGS.PANEL.WARN_BELOW.TOOLTIP, options.WarnBelow,
            on => ModOptions.Instance.WarnBelow = on, go => warnBelow = go,
            "BelowField", c => ModOptions.Instance.BelowCelsius = c, () => ModOptions.Instance.BelowCelsius, go => belowField = go));

        panel.AddChild(CheckBox("PauseGame", STRINGS.PANEL.PAUSE_GAME.NAME, STRINGS.PANEL.PAUSE_GAME.TOOLTIP,
            options.PauseGame, on => ModOptions.Instance.PauseGame = on, go => pauseGame = go));
        panel.AddChild(CheckBox("ShowNotification", STRINGS.PANEL.SHOW_NOTIFICATION.NAME, STRINGS.PANEL.SHOW_NOTIFICATION.TOOLTIP,
            options.ShowNotification, on => ModOptions.Instance.ShowNotification = on, go => showNotification = go));
        panel.AddChild(CheckBox("IncludeAutoSweepers", STRINGS.PANEL.INCLUDE_AUTO_SWEEPERS.NAME, STRINGS.PANEL.INCLUDE_AUTO_SWEEPERS.TOOLTIP,
            options.IncludeAutoSweepers, on => ModOptions.Instance.IncludeAutoSweepers = on, go => includeSweepers = go));

        // Sibling of the vanilla filter panel's content: same parent, same bottom-right corner.
        var parent = ToolMenu.Instance.toolParameterMenu.gameObject;
        root = panel.AddTo(parent);
        var rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = Vector2.zero;
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        rect.sizeDelta = new Vector2(LayoutUtility.GetPreferredWidth(rect), LayoutUtility.GetPreferredHeight(rect));
    }

    // A checkbox followed by its temperature field and unit.
    private static PPanel ThresholdRow(
        string checkName, string label, string tooltip, bool isOn, Action<bool> setOn, Action<GameObject> checkBuilt,
        string fieldName, Action<float> setCelsius, Func<float> getCelsius, Action<GameObject> fieldBuilt)
    {
        var field = new PTextField(fieldName)
        {
            Type = PTextField.FieldType.Float,
            MaxLength = 8,
            MinWidth = 56,
            ToolTip = STRINGS.PANEL.THRESHOLD_TOOLTIP,
            TextStyle = PUITuning.Fonts.TextDarkStyle,
            OnTextChanged = (go, text) => CommitThreshold(go, text, setCelsius, getCelsius),
        }.AddOnRealize(new PUIDelegates.OnRealize(fieldBuilt));

        var unit = new PLabel("Unit") { Text = STRINGS.PANEL.UNIT, TextStyle = PUITuning.Fonts.TextLightStyle, Margin = new RectOffset(0, 8, 0, 0) };

        return new PPanel(checkName + "Row")
        {
            Direction = PanelDirection.Horizontal,
            Spacing = 6,
            Alignment = TextAnchor.MiddleLeft,
            FlexSize = new Vector2(1f, 0f),
        }.AddChild(CheckBox(checkName, label, tooltip, isOn, setOn, checkBuilt, new Vector2(1f, 0f)))
         .AddChild(field)
         .AddChild(unit);
    }

    private static PCheckBox CheckBox(
        string name, string label, string tooltip, bool isOn, Action<bool> setOn, Action<GameObject> built, Vector2? flex = null)
    {
        return new PCheckBox(name)
        {
            Text = label,
            ToolTip = tooltip,
            TextStyle = PUITuning.Fonts.TextLightStyle,
            TextAlignment = TextAnchor.MiddleLeft,
            Margin = new RectOffset(8, 8, 0, 0),
            FlexSize = flex ?? new Vector2(1f, 0f),
            InitialState = isOn ? PCheckBox.STATE_CHECKED : PCheckBox.STATE_UNCHECKED,
            // The handler gets the state from before the click and has to apply the new one itself.
            OnChecked = (go, state) =>
            {
                var on = state != PCheckBox.STATE_CHECKED;
                SetChecked(go, on);
                setOn(on);
                Save();
            },
        }.AddOnRealize(new PUIDelegates.OnRealize(built));
    }

    // Takes the typed temperature if it is a number (clamped to the possible range); otherwise puts the old one back.
    private static void CommitThreshold(GameObject field, string text, Action<float> setCelsius, Func<float> getCelsius)
    {
        if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            && !float.IsNaN(value) && !float.IsInfinity(value))
        {
            setCelsius(Mathf.Clamp(value, MinCelsius, MaxCelsius));
            Save();
        }

        SetField(field, getCelsius());
    }

    private static void SetChecked(GameObject box, bool on)
    {
        if (box != null)
        {
            PCheckBox.SetCheckState(box, on ? PCheckBox.STATE_CHECKED : PCheckBox.STATE_UNCHECKED);
        }
    }

    private static void SetField(GameObject field, float celsius)
    {
        if (field != null)
        {
            field.GetComponent<TMP_InputField>().text = celsius.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }

    private static void Save() => POptions.WriteSettings(ModOptions.Instance);
}
