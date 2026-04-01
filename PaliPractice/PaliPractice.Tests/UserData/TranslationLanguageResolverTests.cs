using System.Globalization;
using PaliPractice.Services.UserData;

namespace PaliPractice.Tests.UserData;

[TestFixture]
public class TranslationLanguageResolverTests
{
    [Test]
    public void EnglishPreference_UsesEnglish()
    {
        var language = TranslationLanguageResolver.ResolveEffectiveLanguageCode(TranslationLanguagePreference.English);

        language.Should().Be(TranslationLanguageResolver.EnglishLanguageCode);
    }

    [Test]
    public void RussianPreference_UsesRussian()
    {
        var language = TranslationLanguageResolver.ResolveEffectiveLanguageCode(TranslationLanguagePreference.Russian);

        language.Should().Be(TranslationLanguageResolver.RussianLanguageCode);
    }

    [Test]
    public void InitialPreference_IsRussianForRussianLocale()
    {
        var preference = TranslationLanguageResolver.GetInitialPreference(new CultureInfo("ru-RU"));

        preference.Should().Be(TranslationLanguagePreference.Russian);
    }

    [Test]
    public void InitialPreference_IsEnglishForNonRussianLocale()
    {
        var preference = TranslationLanguageResolver.GetInitialPreference(new CultureInfo("es-ES"));

        preference.Should().Be(TranslationLanguagePreference.English);
    }

    [Test]
    public void InvalidPreference_FallsBackToInitialPreference()
    {
        var russianPreference = TranslationLanguageResolver.NormalizePreference(999, new CultureInfo("ru-RU"));
        var englishPreference = TranslationLanguageResolver.NormalizePreference(999, new CultureInfo("es-ES"));

        russianPreference.Should().Be(TranslationLanguagePreference.Russian);
        englishPreference.Should().Be(TranslationLanguagePreference.English);
    }
}
