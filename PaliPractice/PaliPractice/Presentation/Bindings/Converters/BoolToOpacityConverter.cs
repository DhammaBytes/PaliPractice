namespace PaliPractice.Presentation.Bindings.Converters;

/// <summary>
/// Converts bool values to Opacity values.
/// true → 1.0 (fully visible), false → 0.0 (invisible but keeps layout space)
/// </summary>
public sealed class BoolToOpacityConverter : IValueConverter
{
    public static readonly BoolToOpacityConverter Instance = new();

    BoolToOpacityConverter() { }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is true ? 1.0 : 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is double d && d > 0.5;
    }
}
