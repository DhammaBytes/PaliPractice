namespace PaliPractice.Themes.Icons;

/// <summary>
/// Helper for navigation icon paths (app bar buttons).
/// Icons are stored as SVGs in Assets/Svg/Navigation/ and rasterized to PNG via Uno.Resizetizer.
/// PNGs are used with BitmapIcon.ShowAsMonochrome for runtime tinting via Foreground brush.
/// </summary>
public static class NavigationIcons
{
    const string BasePath = "ms-appx:///Assets/Svg/Navigation/";

    /// <summary>Back arrow for navigation</summary>
    public static string ArrowBack => $"{BasePath}arrow_back.png";

    /// <summary>History/clock icon</summary>
    public static string History => $"{BasePath}history.png";
}
