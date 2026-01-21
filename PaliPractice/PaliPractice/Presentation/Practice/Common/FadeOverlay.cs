namespace PaliPractice.Presentation.Practice.Common;

/// <summary>
/// A fade overlay that hints at scrollable content.
/// Automatically updates its gradient when the theme changes.
/// </summary>
public sealed class FadeOverlay : Border
{
    readonly GradientStop _backgroundStop;

    public FadeOverlay()
    {
        _backgroundStop = new GradientStop { Color = GetBackgroundColor(), Offset = 1 };

        Height = 24;
        VerticalAlignment = VerticalAlignment.Bottom;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        IsHitTestVisible = false;
        Opacity = 0;
        Background = new LinearGradientBrush
        {
            StartPoint = new Windows.Foundation.Point(0.5, 0),
            EndPoint = new Windows.Foundation.Point(0.5, 1),
            GradientStops =
            {
                new GradientStop { Color = Colors.Transparent, Offset = 0 },
                _backgroundStop
            }
        };

        ActualThemeChanged += OnActualThemeChanged;
    }

    void OnActualThemeChanged(FrameworkElement sender, object args)
    {
        _backgroundStop.Color = GetBackgroundColor();
    }

    static Color GetBackgroundColor() =>
        (Application.Current.Resources["BackgroundBrush"] as SolidColorBrush)?.Color ?? Colors.White;
}
