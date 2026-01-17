namespace PaliPractice.Themes;

/// <summary>
/// Helper for practice page icon paths.
/// Icons are stored as SVGs in Assets/Svg/Practice/ and rasterized to PNG via Uno.Resizetizer.
/// PNGs are used with BitmapIcon.ShowAsMonochrome for runtime tinting via Foreground brush.
/// </summary>
public static class PracticeIcons
{
    const string BasePath = "ms-appx:///Assets/Svg/Practice/";

    /// <summary>Reveal answer button icon (eye)</summary>
    public static string Reveal => $"{BasePath}reveal.png";

    /// <summary>Easy button icon (checkmark)</summary>
    public static string Easy => $"{BasePath}easy.png";

    /// <summary>Hard button icon (X)</summary>
    public static string Hard => $"{BasePath}hard.png";

    /// <summary>Left chevron for translation carousel</summary>
    public static string ChevronLeft => $"{BasePath}chevron_left.png";

    /// <summary>Right chevron for translation carousel</summary>
    public static string ChevronRight => $"{BasePath}chevron_right.png";
}
