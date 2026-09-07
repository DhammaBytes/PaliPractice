using System.Globalization;

namespace PaliPractice.Services.UserData;

public static class TranslationLanguageResolver
{
    public const string EnglishLanguageCode = "en";
    public const string RussianLanguageCode = "ru";
    public const string SpanishLanguageCode = "es";

    public static string NormalizeLanguageCode(string? languageCode) =>
        languageCode?.Split('-', '_')[0].ToLowerInvariant() switch
        {
            RussianLanguageCode => RussianLanguageCode,
            SpanishLanguageCode => SpanishLanguageCode,
            _ => EnglishLanguageCode
        };

    public static string ResolveLanguageCode(CultureInfo? culture = null) =>
        NormalizeLanguageCode((culture ?? CultureInfo.CurrentUICulture).TwoLetterISOLanguageName);

    public static TranslationLanguagePreference GetInitialPreference(CultureInfo? culture = null) =>
        PreferenceFromLanguageCode(ResolveLanguageCode(culture));

    public static TranslationLanguagePreference PreferenceFromLanguageCode(string? languageCode) =>
        NormalizeLanguageCode(languageCode) switch
        {
            RussianLanguageCode => TranslationLanguagePreference.Russian,
            SpanishLanguageCode => TranslationLanguagePreference.Spanish,
            _ => TranslationLanguagePreference.English
        };

    public static TranslationLanguagePreference NormalizePreference(int rawValue, CultureInfo? culture = null) =>
        Enum.IsDefined(typeof(TranslationLanguagePreference), rawValue)
            ? (TranslationLanguagePreference)rawValue
            : GetInitialPreference(culture);

    public static string ResolveEffectiveLanguageCode(TranslationLanguagePreference preference) =>
        preference switch
        {
            TranslationLanguagePreference.Russian => RussianLanguageCode,
            TranslationLanguagePreference.Spanish => SpanishLanguageCode,
            _ => EnglishLanguageCode
        };
}
