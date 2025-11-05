namespace PaliPractice.Presentation.Bindings.Converters;

/// <summary>
/// Converts bool values to Visibility values.
/// When IsInverted is false: true → Visible, false → Collapsed
/// When IsInverted is true: true → Collapsed, false → Visible
/// </summary>
public sealed class BoolToVisibilityConverter : Microsoft.UI.Xaml.Data.IValueConverter
{
    public static readonly BoolToVisibilityConverter Instance = new();

    private BoolToVisibilityConverter() { }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool isInverted = parameter is bool b && b;
        bool boolValue = value is bool bv && bv;

        if (isInverted)
        {
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        }
        else
        {
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        bool isInverted = parameter is bool b && b;
        bool isVisible = value is Visibility v && v == Visibility.Visible;

        return isInverted ? !isVisible : isVisible;
    }
}
