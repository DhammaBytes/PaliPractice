using Uno.Toolkit.UI;

namespace PaliPractice.Presentation.Common;

/// <summary>
/// Helper methods for adding colored drop shadows using Uno Toolkit's ShadowContainer.
/// Shadow colors are defined in ThemeResources.xaml and support light/dark themes.
/// </summary>
public static class ShadowHelper
{
    /// <summary>
    /// Gets a ShadowCollection from the application's theme resources.
    /// </summary>
    static ShadowCollection? GetShadowResource(string resourceKey)
    {
        if (Application.Current.Resources.TryGetValue(resourceKey, out var resource) && resource is ShadowCollection collection)
            return collection;
        return null;
    }

    /// <summary>
    /// Wraps content in a ShadowContainer with a pill button shadow (app bar buttons: Back, History, All Forms).
    /// </summary>
    public static ShadowContainer PillShadow(UIElement content)
    {
        var container = new ShadowContainer { Content = content };
        container.Shadows = GetShadowResource("AppBarNavigationButtonShadow")!;
        return container;
    }

    /// <summary>
    /// Wraps content in a ShadowContainer with a start navigation button shadow (Nouns & Cases, Verbs & Tenses).
    /// </summary>
    public static ShadowContainer StartNavShadow(UIElement content)
    {
        var container = new ShadowContainer { Content = content };
        container.Shadows = GetShadowResource("StartNavigationButtonVariantShadow")!;
        return container;
    }

    /// <summary>
    /// Wraps content in a ShadowContainer with a Hard/Easy button shadow.
    /// </summary>
    public static ShadowContainer HardEasyShadow(UIElement content)
    {
        var container = new ShadowContainer { Content = content };
        container.Shadows = GetShadowResource("StartNavigationButtonShadow")!;
        return container;
    }

    /// <summary>
    /// Wraps content in a ShadowContainer with a secondary button shadow (Settings, Stats, Help on Start page).
    /// </summary>
    public static ShadowContainer StartNavigationButtonShadow(UIElement content)
    {
        var container = new ShadowContainer { Content = content };
        container.Shadows = GetShadowResource("StartNavigationButtonShadow")!;
        return container;
    }

    /// <summary>
    /// Wraps content in a ShadowContainer with a card shadow (practice card, translation box).
    /// </summary>
    public static ShadowContainer CardShadow(UIElement content)
    {
        var container = new ShadowContainer { Content = content };
        container.Shadows = GetShadowResource("CardShadow")!;
        return container;
    }
}
