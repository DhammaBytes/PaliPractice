namespace PaliPractice.Presentation.Bindings.Converters;

/// <summary>
/// Converts a string path (e.g., "ms-appx:///Assets/Icons/badge.png") to a Uri.
/// Used for BitmapIcon.UriSource binding.
/// Caches created Uri objects to avoid repeated allocations.
/// </summary>
public sealed class StringToUriConverter : IValueConverter
{
    public static readonly StringToUriConverter Instance = new();

    // Cache URIs to avoid allocations
    static readonly Dictionary<string, Uri> _uriCache = new();

    public object? Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is not string path || string.IsNullOrEmpty(path))
            return null;

        if (!_uriCache.TryGetValue(path, out var uri))
        {
            uri = new Uri(path);
            _uriCache[path] = uri;
        }
        return uri;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        // One-way conversion only
        throw new NotSupportedException();
    }
}
