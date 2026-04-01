using System.Globalization;

namespace PaliPractice.Services.UserData;

public static class TranslationLanguageResolver
{
    public const string EnglishLanguageCode = "en";
    public const string RussianLanguageCode = "ru";

    public static TranslationLanguagePreference GetInitialPreference(CultureInfo? culture = null)
    {
        var effectiveCulture = culture ?? CultureInfo.CurrentUICulture;
        return string.Equals(effectiveCulture.TwoLetterISOLanguageName, RussianLanguageCode, StringComparison.OrdinalIgnoreCase)
            ? TranslationLanguagePreference.Russian
            : TranslationLanguagePreference.English;
    }

    public static TranslationLanguagePreference NormalizePreference(int rawValue, CultureInfo? culture = null)
    {
        return Enum.IsDefined(typeof(TranslationLanguagePreference), rawValue)
            ? (TranslationLanguagePreference)rawValue
            : GetInitialPreference(culture);
    }

    public static string ResolveEffectiveLanguageCode(TranslationLanguagePreference preference)
    {
        return preference == TranslationLanguagePreference.Russian
            ? RussianLanguageCode
            : EnglishLanguageCode;
    }
}
