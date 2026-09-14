// ShadowContainer lives in the Skia toolkit assembly, which has no bundled markup extensions.
[assembly: Uno.Extensions.Markup.Generator.GenerateMarkupForAssembly(typeof(ShadowContainer))]

namespace PaliPractice.Presentation.Common;

/// <summary>
/// Helper methods for adding colored drop shadows using Uno Toolkit's ShadowContainer.
/// Shadow colors are defined in ThemeResources.xaml and support light/dark themes.
/// Shadows automatically update when the app theme changes.
/// </summary>
public static class ShadowHelper
{
    static ShadowContainer CreateThemeAwareShadowContainer(UIElement content, string shadowResourceKey)
        => new ShadowContainer()
            .Shadows(ThemeResource.Get<ShadowCollection>(shadowResourceKey))
            .Content(content);

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
