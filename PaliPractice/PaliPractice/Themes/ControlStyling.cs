namespace PaliPractice.Themes;

/// <summary>
/// Applies accent color overrides to WinUI controls.
/// Reads brushes from ThemeResources.xaml and applies to control-specific resources.
/// Must be called after XamlControlsResources and ThemeResources are loaded.
/// </summary>
public static class ControlStyling
{
    // https://platform.uno/docs/articles/external/uno.themes/doc/lightweight-styling.html#resource-keys
    
    /// <summary>
    /// Applies accent colors from theme resources to WinUI controls.
    /// </summary>
    /// <param name="resources">The application's resource dictionary.</param>
    public static void ApplyAccentColorOverrides(ResourceDictionary resources)
    {
        // Read accent brushes from theme (defined in ThemeResources.xaml)
        var accent = resources["AccentBrush"];
        var accentHover = resources["AccentHoverBrush"];
        var accentPressed = resources["AccentPressedBrush"];
        var accentSubtle = resources["AccentSubtleBrush"];
        var accentSubtleHover = resources["AccentSubtleHoverBrush"];

        // ToggleSwitch
        resources["ToggleSwitchFillOn"] = accent;
        resources["ToggleSwitchFillOnPointerOver"] = accentHover;
        resources["ToggleSwitchFillOnPressed"] = accentPressed;

        // CheckBox
        resources["CheckBoxCheckBackgroundFillChecked"] = accent;
        resources["CheckBoxCheckBackgroundFillCheckedPointerOver"] = accentHover;
        resources["CheckBoxCheckBackgroundFillCheckedPressed"] = accentPressed;
        resources["CheckBoxCheckBackgroundStrokeChecked"] = accent;
        resources["CheckBoxCheckBackgroundStrokeCheckedPointerOver"] = accentHover;
        resources["CheckBoxCheckBackgroundStrokeCheckedPressed"] = accentPressed;

        // RadioButton
        resources["RadioButtonOuterEllipseFillChecked"] = accent;
        resources["RadioButtonOuterEllipseFillCheckedPointerOver"] = accentHover;
        resources["RadioButtonOuterEllipseFillCheckedPressed"] = accentPressed;
        resources["RadioButtonOuterEllipseStrokeChecked"] = accent;
        resources["RadioButtonOuterEllipseStrokeCheckedPointerOver"] = accentHover;
        resources["RadioButtonOuterEllipseStrokeCheckedPressed"] = accentPressed;

        // ComboBox
        resources["ComboBoxBackgroundFocused"] = accentSubtle;
        resources["ComboBoxBorderBrushFocused"] = accent;
        resources["ComboBoxDropDownBackgroundPointerOver"] = accentSubtleHover;

        // TextBox/input focus
        resources["TextControlBorderBrushFocused"] = accent;

        // Slider
        resources["SliderTrackValueFill"] = accent;
        resources["SliderTrackValueFillPointerOver"] = accentHover;
        resources["SliderTrackValueFillPressed"] = accentPressed;
        resources["SliderThumbBackground"] = accent;
        resources["SliderThumbBackgroundPointerOver"] = accentHover;
        resources["SliderThumbBackgroundPressed"] = accentPressed;

        // Hyperlink - use dedicated link colors for better visibility
        var link = resources["LinkBrush"];
        var linkHover = resources["LinkHoverBrush"];
        var linkPressed = resources["LinkPressedBrush"];
        resources["HyperlinkForeground"] = link;
        resources["HyperlinkForegroundPointerOver"] = linkHover;
        resources["HyperlinkForegroundPressed"] = linkPressed;
    }
}
