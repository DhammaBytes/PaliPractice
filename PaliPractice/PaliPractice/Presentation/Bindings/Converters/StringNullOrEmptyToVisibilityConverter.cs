namespace PaliPractice.Presentation.Bindings.Converters;

/// <summary>
/// Converts string values to Visibility based on null/empty check.
/// Empty or null strings → Collapsed
/// Non-empty strings → Visible
/// </summary>
public sealed class StringNullOrEmptyToVisibilityConverter : IValueConverter
{
    public static readonly StringNullOrEmptyToVisibilityConverter Instance = new();

    StringNullOrEmptyToVisibilityConverter() { }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var isEmpty = string.IsNullOrEmpty(value as string);
        var invert = parameter is true;

        return (isEmpty ^ invert) ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return DependencyProperty.UnsetValue;
    }
}
