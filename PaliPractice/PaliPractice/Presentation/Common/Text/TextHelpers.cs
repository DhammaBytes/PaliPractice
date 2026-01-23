using PaliPractice.Themes;

namespace PaliPractice.Presentation.Common.Text;

/// <summary>
/// Factory methods for creating TextBlocks with predefined fonts.
/// Use these instead of raw TextBlock() to ensure consistent font usage.
/// Add "using static PaliPractice.Presentation.Common.TextHelpers;" to use.
/// </summary>
public static class TextHelpers
{
    /// <summary>
    /// Cached FontFamily for Pāli text (LibertinusSans).
    /// Static caching prevents GC from collecting font while Skia render thread is using it.
    /// </summary>
    public static readonly FontFamily PaliFont = new(FontPaths.LibertinusSans);

    /// <summary>
    /// Cached FontFamily for UI text (SourceSans3).
    /// Static caching prevents GC from collecting font while Skia render thread is using it.
    /// </summary>
    public static readonly FontFamily RegularFont = new(FontPaths.SourceSans);

    /// <summary>
    /// Creates a TextBlock with LibertinusSans font for Pāli text.
    /// Serif-like pronounced rhythm helps with letter-by-letter decoding of unfamiliar words.
    /// </summary>
    public static TextBlock PaliText() => new TextBlock()
        .FontFamily(FontPaths.LibertinusSans);

    /// <summary>
    /// Creates a TextBlock with SourceSans3 font for UI elements and translations.
    /// </summary>
    public static TextBlock RegularText() => new TextBlock()
        .FontFamily(FontPaths.SourceSans);
}
