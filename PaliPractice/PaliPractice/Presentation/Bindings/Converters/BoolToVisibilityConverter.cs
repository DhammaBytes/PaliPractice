namespace PaliPractice.Presentation.Bindings.Converters;

/// <summary>
/// Converts bool values to Visibility values.
/// When IsInverted is false: true → Visible, false → Collapsed
/// When IsInverted is true: true → Collapsed, false → Visible
/// </summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public static readonly BoolToVisibilityConverter Instance = new();

    BoolToVisibilityConverter() { }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool isInverted = parameter is true;
        bool boolValue = value is true;

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
        bool isInverted = parameter is true;
        bool isVisible = value is Visibility.Visible;

        return isInverted ? !isVisible : isVisible;
    }
}
