namespace PaliPractice.Presentation.Practice.Common;

/// <summary>
/// A fade overlay that hints at scrollable content.
/// Automatically updates its gradient when the theme changes.
/// </summary>
/// <remarks>
/// Must recreate entire brush on theme change - Uno doesn't re-render when GradientStop.Color changes.
/// See: https://github.com/unoplatform/uno/issues/6642
/// </remarks>
public sealed class FadeOverlay : Border
{
    public FadeOverlay()
    {
        Height = 24;
        VerticalAlignment = VerticalAlignment.Bottom;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        IsHitTestVisible = false;
        Opacity = 0;

        Loaded += OnLoaded;
        ActualThemeChanged += OnActualThemeChanged;
    }

    void OnLoaded(object sender, RoutedEventArgs e) => RecreateBrush();
    void OnActualThemeChanged(FrameworkElement sender, object args) => RecreateBrush();

    void RecreateBrush()
    {
        var bgColor = GetThemedColor("BackgroundBrush");
        var bgColorTransparent = Color.FromArgb(0, bgColor.R, bgColor.G, bgColor.B);

        Background = new LinearGradientBrush
        {
            StartPoint = new Windows.Foundation.Point(0.5, 0),
            EndPoint = new Windows.Foundation.Point(0.5, 1),
            GradientStops =
            {
                new GradientStop { Color = bgColorTransparent, Offset = 0 },
                new GradientStop { Color = bgColor, Offset = 1 }
            }
        };
    }

    Color GetThemedColor(string key)
    {
        var themeKey = ActualTheme == ElementTheme.Dark ? "Dark" : "Light";

        // Search in merged dictionaries (ThemeResources.xaml is merged)
        foreach (var merged in Application.Current.Resources.MergedDictionaries)
        {
            if (merged.ThemeDictionaries.TryGetValue(themeKey, out var dict) &&
                dict is ResourceDictionary rd &&
                rd.TryGetValue(key, out var resource) &&
                resource is SolidColorBrush brush)
            {
                return brush.Color;
            }
        }

        throw new InvalidOperationException($"Theme resource '{key}' not found for theme '{themeKey}'");
    }
}
