namespace PaliPractice.Presentation.Common;

/// <summary>
/// Helper methods for adding drop shadows to UI elements using WinUI's ThemeShadow.
/// Note: ThemeShadow doesn't support custom colors, but provides consistent visual depth.
/// </summary>
public static class ShadowHelper
{
    /// <summary>
    /// Adds a ThemeShadow to an element by setting Translation on the Z-axis.
    /// The shadow is rendered automatically by WinUI on Skia platforms.
    /// </summary>
    /// <param name="element">The element to add shadow to.</param>
    /// <param name="elevation">Z-axis elevation in pixels (affects shadow spread/blur).</param>
    /// <returns>The same element with shadow applied.</returns>
    public static T WithShadow<T>(this T element, double elevation = 16) where T : UIElement
    {
        if (element is FrameworkElement fe)
        {
            fe.Translation = new System.Numerics.Vector3(0, 0, (float)elevation);
            fe.Shadow = new ThemeShadow();
        }
        return element;
    }

    /// <summary>
    /// Adds a pill button shadow (app bar buttons: Back, History, All Forms).
    /// </summary>
    public static T WithPillShadow<T>(this T element, double cornerRadius = 14) where T : UIElement
    {
        return element.WithShadow(elevation: 8);
    }

    /// <summary>
    /// Adds a start navigation button shadow (Nouns & Cases, Verbs & Tenses).
    /// </summary>
    public static T WithStartNavShadow<T>(this T element, double cornerRadius = 14) where T : UIElement
    {
        return element.WithShadow(elevation: 12);
    }

    /// <summary>
    /// Adds a Hard/Easy button shadow.
    /// </summary>
    public static T WithHardEasyShadow<T>(this T element, double cornerRadius = 14) where T : UIElement
    {
        return element.WithShadow(elevation: 8);
    }

    /// <summary>
    /// Adds a secondary button shadow (Settings, Stats, Help on Start page).
    /// </summary>
    public static T WithSecondaryButtonShadow<T>(this T element, double cornerRadius = 14) where T : UIElement
    {
        return element.WithPillShadow(cornerRadius);
    }

    /// <summary>
    /// Adds a card shadow (practice card, translation box, arrow buttons).
    /// Based on Figma: X=0, Y=10, Blur=20, Color #F1D6C2 100%.
    /// </summary>
    public static T WithCardShadow<T>(this T element, double cornerRadius = 14) where T : UIElement
    {
        return element.WithShadow(elevation: 20);
    }
}
