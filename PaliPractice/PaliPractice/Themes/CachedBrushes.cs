namespace PaliPractice.Themes;

/// <summary>
/// Cached brush instances for common solid colors.
/// Static caching prevents GC from collecting brushes while Skia render thread uses them.
/// </summary>
public static class CachedBrushes
{
    /// <summary>Transparent brush for invisible backgrounds/hit areas.</summary>
    public static readonly SolidColorBrush Transparent = new(Colors.Transparent);
}
