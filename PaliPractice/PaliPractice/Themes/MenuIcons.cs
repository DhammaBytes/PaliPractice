namespace PaliPractice.Themes;

/// <summary>
/// Helper for menu icon paths on the start page.
/// Icons are stored as SVGs in Assets/Svg/Menu/ and rasterized to PNG via Uno.Resizetizer.
/// PNGs are used with BitmapIcon.ShowAsMonochrome for runtime tinting via Foreground brush.
/// </summary>
public static class MenuIcons
{
    const string BasePath = "ms-appx:///Assets/Svg/Menu/";

    public static string Nouns => $"{BasePath}nouns.png";
    public static string Verbs => $"{BasePath}verbs.png";
    public static string Settings => $"{BasePath}settings.png";
    public static string Stats => $"{BasePath}stats.png";
    public static string Help => $"{BasePath}help.png";
}
