namespace PaliPractice.Presentation.Bindings.Converters;

/// <summary>
/// Converts a value to a bool by comparing it to a target value.
/// Used for binding enum values to ToggleButton.IsChecked.
/// </summary>
public sealed class EqualityConverter<T> : IValueConverter where T : notnull
{
    static readonly Dictionary<T, EqualityConverter<T>> Cache = new();

    public T Target { get; private set; } = default!;

    EqualityConverter() { }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return EqualityComparer<T>.Default.Equals((T)value, Target);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return DependencyProperty.UnsetValue;
    }

    /// <summary>
    /// Gets a cached converter instance for the given target value.
    /// Reduces allocations when creating many toggle buttons with the same enum values.
    /// </summary>
    public static EqualityConverter<T> For(T target)
    {
        if (!Cache.TryGetValue(target, out var converter))
        {
            converter = new EqualityConverter<T> { Target = target };
            Cache[target] = converter;
        }
        return converter;
    }
}
