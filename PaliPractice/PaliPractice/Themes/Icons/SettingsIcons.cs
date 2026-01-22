namespace PaliPractice.Themes.Icons;

/// <summary>
/// Helper for settings icon paths.
/// Icons are stored as SVGs in Assets/Svg/Settings/ and rasterized to PNG via Uno.Resizetizer.
/// PNGs are used with BitmapIcon.ShowAsMonochrome for runtime tinting via Foreground brush.
/// </summary>
public static class SettingsIcons
{
    const string BasePath = "ms-appx:///Assets/Svg/Settings/";

    public static string About => $"{BasePath}about.png";
    public static string Appearance => $"{BasePath}appearance.png";
    public static string Contact => $"{BasePath}contact.png";
    public static string Noun => $"{BasePath}noun.png";
    public static string Rate => $"{BasePath}rate.png";
    public static string Verb => $"{BasePath}verb.png";
}
