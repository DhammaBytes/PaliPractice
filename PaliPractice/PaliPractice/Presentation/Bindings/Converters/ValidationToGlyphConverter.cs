using PaliPractice.Presentation.ViewModels;

namespace PaliPractice.Presentation.Bindings.Converters;

/// <summary>
/// Converts ValidationState to appropriate Segoe MDL2 Assets glyph.
/// Correct → Check mark (✓)
/// Incorrect → Cancel/Cross (✗)
/// Unknown → Empty string
/// </summary>
public sealed class ValidationToGlyphConverter : IValueConverter
{
    public static readonly ValidationToGlyphConverter Instance = new();

    ValidationToGlyphConverter() { }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is ValidationState s
            ? s switch
            {
                ValidationState.Correct => "\uE73E",   // CheckMark
                ValidationState.Incorrect => "\uE711", // Cancel
                _ => ""                                 // No glyph
            }
            : "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return DependencyProperty.UnsetValue;
    }
}
