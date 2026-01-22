namespace PaliPractice.Presentation.Common;

/// <summary>
/// Helper methods for adding colored drop shadows using Uno Toolkit's ShadowContainer.
/// Shadow colors are defined in ThemeResources.xaml and support light/dark themes.
/// Shadows automatically update when the app theme changes.
/// </summary>
public static class ShadowHelper
{
    /// <summary>
    /// Gets a ShadowCollection from the current theme's resources.
    /// Uses the proper theme lookup to get the correct shadow for light/dark mode.
    /// </summary>
    static ShadowCollection? GetShadowResource(string resourceKey, FrameworkElement element)
    {
        // Use the element's resolved theme to get the correct resource
        // This properly handles ThemeDictionaries with Light/Dark variants
        if (element.Resources.TryGetValue(resourceKey, out var localResource) && localResource is ShadowCollection localCollection)
            return localCollection;

        // Fall back to application resources with theme awareness
        if (Application.Current.Resources.ThemeDictionaries.TryGetValue(
                element.ActualTheme == ElementTheme.Dark ? "Dark" : "Light",
                out var themeDict) &&
            themeDict is ResourceDictionary dict &&
            dict.TryGetValue(resourceKey, out var resource) &&
            resource is ShadowCollection collection)
        {
            return collection;
        }

        // Final fallback: direct lookup (for non-themed resources)
        if (Application.Current.Resources.TryGetValue(resourceKey, out var fallback) && fallback is ShadowCollection fallbackCollection)
            return fallbackCollection;

        return null;
    }

    /// <summary>
    /// Creates a theme-aware ShadowContainer that updates shadows when the theme changes.
    /// </summary>
    static ShadowContainer CreateThemeAwareShadowContainer(UIElement content, string shadowResourceKey)
    {
        var container = new ShadowContainer { Content = content };

        // Apply initial shadow once loaded (so we can access ActualTheme)
        container.Loaded += OnLoaded;

        // Update shadow when theme changes
        container.ActualThemeChanged += OnThemeChanged;

        return container;

        void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is ShadowContainer sc)
            {
                var shadow = GetShadowResource(shadowResourceKey, sc);
                if (shadow != null)
                    sc.Shadows = shadow;
            }
        }

        void OnThemeChanged(FrameworkElement sender, object args)
        {
            if (sender is ShadowContainer sc)
            {
                var shadow = GetShadowResource(shadowResourceKey, sc);
                if (shadow != null)
                    sc.Shadows = shadow;
            }
        }
    }

    /// <summary>
    /// Wraps content in a ShadowContainer with a pill button shadow (app bar buttons: Back, History, All Forms).
    /// </summary>
    public static ShadowContainer NavigationButtonShadow(UIElement content)
        => CreateThemeAwareShadowContainer(content, "NavigationButtonShadow");
    
    public static ShadowContainer StartPrimaryButtonShadow(UIElement content)
        => CreateThemeAwareShadowContainer(content, "StartPrimaryButtonShadow");
    
    public static ShadowContainer StartSecondaryButtonShadow(UIElement content)
        => CreateThemeAwareShadowContainer(content, "StartSecondaryButtonShadow");
    
    public static ShadowContainer ButtonShadow(UIElement content)
        => CreateThemeAwareShadowContainer(content, "ButtonShadow");
    
    public static ShadowContainer CardShadow(UIElement content)
        => CreateThemeAwareShadowContainer(content, "CardShadow");
}
