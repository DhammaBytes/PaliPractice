namespace PaliPractice.Themes;

/// <summary>
/// Font path constants for use with FontFamily.
/// These reference fonts in Assets/Fonts/ with manifests for cross-platform weight support.
/// </summary>
public static class FontPaths
{
    /// <summary>
    /// Libertinus Sans - used for Pāli text.
    /// Serif-like pronounced rhythm helps with letter-by-letter decoding.
    /// Weights: Regular (400), Bold (700)
    /// </summary>
    public const string LibertinusSans = "ms-appx:///Assets/Fonts/LibertinusSans.ttf";

    /// <summary>
    /// Source Sans 3 - used for UI elements.
    /// Weights: Regular (400), Italic (400i), Medium (500), SemiBold (600), Bold (700)
    /// </summary>
    public const string SourceSans = "ms-appx:///Assets/Fonts/SourceSans3.ttf";
}
