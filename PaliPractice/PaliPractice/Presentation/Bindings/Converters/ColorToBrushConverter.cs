namespace PaliPractice.Presentation.Bindings.Converters;

/// <summary>
/// Converts a Color value to a SolidColorBrush.
/// Used for binding Color properties in ViewModels to Brush properties in Views.
/// </summary>
public sealed class ColorToBrushConverter : IValueConverter
{
    public static readonly ColorToBrushConverter Instance = new();

    // Cache brushes to avoid allocations
    static readonly Dictionary<Color, SolidColorBrush> _brushCache = new();

    public object? Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is Color color)
        {
            if (!_brushCache.TryGetValue(color, out var brush))
            {
                brush = new SolidColorBrush(color);
                _brushCache[color] = brush;
            }
            return brush;
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        if (value is SolidColorBrush brush)
            return brush.Color;
        return Colors.Transparent;
    }
}
