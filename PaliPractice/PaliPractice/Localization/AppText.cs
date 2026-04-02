using System.Globalization;
using System.Xml.Linq;
using PaliPractice.Services.UserData;
using Windows.ApplicationModel.Resources;

namespace PaliPractice.Localization;

public static class AppText
{
    static readonly Lazy<IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>> FallbackResources =
        new(LoadFallbackResources);

    static ResourceLoader? _loader;
    static bool _resourceLoaderUnavailable;

    public static string Get(string key)
    {
        var value = TryGetFromResourceLoader(key) ?? TryGetFromFallbackResources(key);
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
            var value = _loader.GetString(key);
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        catch (Exception ex) when (IsLoaderUnavailableException(ex))
        {
            _resourceLoaderUnavailable = true;
            return null;
        }
    }

    static string? TryGetFromFallbackResources(string key)
    {
        var resources = FallbackResources.Value;
        var languageCode = TranslationLanguageResolver.ResolveLanguageCode();

        if (resources.TryGetValue(languageCode, out var localized) && localized.TryGetValue(key, out var value))
            return value;

        return resources.TryGetValue(TranslationLanguageResolver.EnglishLanguageCode, out var english)
            && english.TryGetValue(key, out value)
            ? value
            : null;
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

    static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> LoadFallbackResources()
    {
        return new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            [TranslationLanguageResolver.EnglishLanguageCode] = LoadLanguageResources(TranslationLanguageResolver.EnglishLanguageCode),
            [TranslationLanguageResolver.RussianLanguageCode] = LoadLanguageResources(TranslationLanguageResolver.RussianLanguageCode)
        };
    }

    static IReadOnlyDictionary<string, string> LoadLanguageResources(string languageCode)
    {
        var path = FindResourcesPath(languageCode);
        if (path is null)
            return new Dictionary<string, string>(StringComparer.Ordinal);

        var document = XDocument.Load(path);
        return document.Root?
            .Elements("data")
            .Select(element => new
            {
                Key = element.Attribute("name")?.Value,
                Value = element.Element("value")?.Value
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Key) && item.Value is not null)
            .ToDictionary(item => item.Key!, item => item.Value!, StringComparer.Ordinal)
            ?? new Dictionary<string, string>(StringComparer.Ordinal);
    }

    static string? FindResourcesPath(string languageCode)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            foreach (var candidate in GetResourcePathCandidates(directory.FullName, languageCode))
            {
                if (File.Exists(candidate))
                    return candidate;
            }
        }

        return null;
    }

    static IEnumerable<string> GetResourcePathCandidates(string baseDirectory, string languageCode)
    {
        yield return System.IO.Path.Combine(baseDirectory, "Strings", languageCode, "Resources.resw");
        yield return System.IO.Path.Combine(baseDirectory, "PaliPractice", "Strings", languageCode, "Resources.resw");
        yield return System.IO.Path.Combine(baseDirectory, "PaliPractice", "PaliPractice", "Strings", languageCode, "Resources.resw");
    }
}
