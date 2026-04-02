using System.Globalization;
using Windows.ApplicationModel.Resources;

namespace PaliPractice.Localization;

public static class AppText
{
    static ResourceLoader? _loader;
    static bool _resourceLoaderUnavailable;

    public static string Get(string key)
    {
        var value = TryGetFromResourceLoader(key);
        return string.IsNullOrWhiteSpace(value) ? $"[{key}]" : value;
    }

    public static string Format(string key, params object?[] args)
        => string.Format(CultureInfo.CurrentUICulture, Get(key), args);

    static string? TryGetFromResourceLoader(string key)
    {
        if (_resourceLoaderUnavailable)
            return null;

        try
        {
            _loader ??= ResourceLoader.GetForViewIndependentUse();
            var value = _loader.GetString(ToResourceLoaderKey(key));
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        catch (Exception ex) when (IsLoaderUnavailableException(ex))
        {
            _resourceLoaderUnavailable = true;
            return null;
        }
    }

    static bool IsLoaderUnavailableException(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is TypeInitializationException or NullReferenceException or InvalidOperationException)
                return true;
        }

        return false;
    }

    internal static string ToResourceLoaderKey(string key)
    {
        // .resw authoring commonly uses dot-separated names such as "App.Name" or
        // "Settings.Row.Language", but ResourceLoader resolves path-like identifiers.
        // In Uno/WinUI resource compilation, only the first dot becomes a '/' segment
        // separator, so "App.Name" -> "App/Name" and
        // "Settings.Row.Language" -> "Settings/Row.Language".
        // Do not add a leading '/': ResourceLoader treats "/[file]/[name]" as the
        // special syntax for targeting a named loader, not a normal key in Resources.
        if (string.IsNullOrEmpty(key) || key.Contains('/'))
            return key;

        var firstDotIndex = key.IndexOf('.');
        if (firstDotIndex < 0)
            return key;

        return string.Concat(key.AsSpan(0, firstDotIndex), "/", key.AsSpan(firstDotIndex + 1));
    }
}
