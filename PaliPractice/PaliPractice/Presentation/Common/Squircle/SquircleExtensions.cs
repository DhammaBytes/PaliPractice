namespace PaliPractice.Presentation.Common.Squircle;

/// <summary>
/// C# Markup extension methods for SquircleBorder and SquircleButton.
/// </summary>
public static class SquircleExtensions
{
    #region SquircleButton Extensions

    /// <summary>
    /// Sets the radius mode for automatic radius calculation.
    /// </summary>
    public static SquircleButton RadiusMode(this SquircleButton button, SquircleRadiusMode mode)
    {
        button.RadiusMode = mode;
        return button;
    }

    /// <summary>
    /// Sets the fill brush.
    /// </summary>
    public static SquircleButton Fill(this SquircleButton button, Brush fill)
    {
        button.Fill = fill;
        return button;
    }

    /// <summary>
    /// Sets the content of the squircle button (convenience wrapper for Button.Content).
    /// </summary>
    public static SquircleButton Child(this SquircleButton button, UIElement child)
    {
        button.Content = child;
        return button;
    }

    #endregion

    #region SquircleBorder Extensions

    /// <summary>
    /// Sets the corner radius of the squircle.
    /// </summary>
    public static SquircleBorder Radius(this SquircleBorder border, double radius)
    {
        border.Radius = radius;
        return border;
    }

    /// <summary>
    /// Sets which corners should be rounded.
    /// </summary>
    public static SquircleBorder Corners(this SquircleBorder border, SquircleCorners corners)
    {
        border.Corners = corners;
        return border;
    }

    /// <summary>
    /// Sets the radius mode for automatic radius calculation.
    /// </summary>
    public static SquircleBorder RadiusMode(this SquircleBorder border, SquircleRadiusMode mode)
    {
        border.RadiusMode = mode;
        return border;
    }

    /// <summary>
    /// Sets the fill brush.
    /// </summary>
    public static SquircleBorder Fill(this SquircleBorder border, Brush fill)
    {
        border.Fill = fill;
        return border;
    }

    /// <summary>
    /// Sets the stroke brush.
    /// </summary>
    public static SquircleBorder Stroke(this SquircleBorder border, Brush stroke)
    {
        border.Stroke = stroke;
        return border;
    }

    /// <summary>
    /// Sets the stroke thickness.
    /// </summary>
    public static SquircleBorder StrokeThickness(this SquircleBorder border, double thickness)
    {
        border.StrokeThickness = thickness;
        return border;
    }

    /// <summary>
    /// Sets the content of the squircle border.
    /// </summary>
    public static SquircleBorder Child(this SquircleBorder border, UIElement child)
    {
        border.Content = child;
        return border;
    }

    #endregion
}
