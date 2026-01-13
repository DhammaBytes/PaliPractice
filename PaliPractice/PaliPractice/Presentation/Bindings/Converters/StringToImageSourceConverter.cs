using Microsoft.UI.Xaml.Media.Imaging;

namespace PaliPractice.Presentation.Bindings.Converters;

/// <summary>
/// Converts a string path (e.g., "ms-appx:///Assets/Svg/icon.svg") to an ImageSource.
/// Automatically uses SvgImageSource for .svg files and BitmapImage for other formats.
/// Caches created ImageSource objects to avoid repeated allocations.
/// </summary>
public sealed class StringToImageSourceConverter : IValueConverter
{
    public static readonly StringToImageSourceConverter Instance = new();

    // Cache image sources to avoid allocations
    static readonly Dictionary<string, ImageSource> _sourceCache = new();

    public object? Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is not string path || string.IsNullOrEmpty(path))
            return null;

        if (!_sourceCache.TryGetValue(path, out var source))
        {
            var uri = new Uri(path);
            source = path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)
                ? new SvgImageSource(uri)
                : new BitmapImage(uri);
            _sourceCache[path] = source;
        }
        return source;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        // One-way conversion only
        throw new NotSupportedException();
    }
}
