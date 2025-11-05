namespace PaliPractice.Presentation.Bindings.Converters;

/// <summary>
/// Converts string values to Visibility based on null/empty check.
/// Empty or null strings → Collapsed
/// Non-empty strings → Visible
/// </summary>
public sealed class StringNullOrEmptyToVisibilityConverter : Microsoft.UI.Xaml.Data.IValueConverter
{
    public static readonly StringNullOrEmptyToVisibilityConverter Instance = new();

    private StringNullOrEmptyToVisibilityConverter() { }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return DependencyProperty.UnsetValue;
    }
}
