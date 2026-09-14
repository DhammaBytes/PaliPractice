using System.Globalization;
using Windows.System.UserProfile;

namespace PaliPractice.Localization;

internal static class WindowsSystemLanguage
{
    public static void Apply()
    {
        if (!OperatingSystem.IsWindows()) return;

        // The user's preferred-language list is separate from regional formats
        // and from the app's runtime language list, which may contain an old override.
        IReadOnlyList<string> languages;
        try
        {
            languages = GlobalizationPreferences.Languages;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Windows language preference read failed: {ex}");
            languages = [CultureInfo.CurrentUICulture.Name];
        }

        var culture = languages
            .Select(TryGetSupportedCulture)
            .FirstOrDefault(candidate => candidate is not null)
            ?? TryGetSupportedCulture(CultureInfo.CurrentUICulture.Name)
            ?? CultureInfo.GetCultureInfo("en");

        try
        {
            Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = culture.Name;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Windows language override failed: {ex}");
        }

        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }

    static CultureInfo? TryGetSupportedCulture(string languageTag)
    {
        try
        {
            var culture = CultureInfo.GetCultureInfo(languageTag);
            return culture.TwoLetterISOLanguageName is "en" or "es" or "ru" ? culture : null;
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }
}
