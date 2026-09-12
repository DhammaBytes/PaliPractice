using System.Globalization;
using System.Text.Json;
using PaliPractice.Services.UserData;

namespace PaliPractice.Tests.UserData;

[TestFixture]
public class TranslationLanguageResolverTests
{
    [TestCase("en-US")]
    [TestCase("ru-RU")]
    [TestCase("es-ES")]
    [TestCase("es-MX")]
    public void StartupConfigurationPreservesSupportedTranslationLocales(string locale)
    {
        var path = System.IO.Path.Combine(TestPaths.RepositoryRoot, "PaliPractice", "PaliPractice", "appsettings.json");
        using var configuration = JsonDocument.Parse(File.ReadAllText(path));
        var cultures = configuration.RootElement.GetProperty("LocalizationConfiguration")
            .GetProperty("Cultures").EnumerateArray().Select(value => value.GetString()).ToArray();
        var deviceCulture = new CultureInfo(locale);

        cultures.Should().Contain(deviceCulture.TwoLetterISOLanguageName,
            "startup must preserve the device language before creating dictionary preferences");
        TranslationLanguageResolver.GetInitialPreference(deviceCulture).Should().Be(
            TranslationLanguageResolver.PreferenceFromLanguageCode(deviceCulture.TwoLetterISOLanguageName));
    }

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
    public void InitialPreference_IsSpanishForSpanishLocale()
    {
        var preference = TranslationLanguageResolver.GetInitialPreference(new CultureInfo("es-ES"));

        preference.Should().Be(TranslationLanguagePreference.Spanish);
    }

    [Test]
    public void InvalidPreference_FallsBackToInitialPreference()
    {
        var russianPreference = TranslationLanguageResolver.NormalizePreference(999, new CultureInfo("ru-RU"));
        var englishPreference = TranslationLanguageResolver.NormalizePreference(999, new CultureInfo("de-DE"));

        russianPreference.Should().Be(TranslationLanguagePreference.Russian);
        englishPreference.Should().Be(TranslationLanguagePreference.English);
    }
    [TestCase("es-ES")]
    [TestCase("es-MX")]
    [TestCase("es-AR")]
    public void SpanishLocalesSelectSpanish(string locale) =>
        TranslationLanguageResolver.GetInitialPreference(new CultureInfo(locale)).Should().Be(TranslationLanguagePreference.Spanish);

    [Test]
    public void StoredValuesRemainStableAndOverrideLocale()
    {
        ((int)TranslationLanguagePreference.English).Should().Be(0);
        ((int)TranslationLanguagePreference.Russian).Should().Be(1);
        ((int)TranslationLanguagePreference.Spanish).Should().Be(2);
        TranslationLanguageResolver.NormalizePreference(0, new CultureInfo("es-MX")).Should().Be(TranslationLanguagePreference.English);
        TranslationLanguageResolver.NormalizePreference(1, new CultureInfo("es-MX")).Should().Be(TranslationLanguagePreference.Russian);
        TranslationLanguageResolver.ResolveEffectiveLanguageCode(TranslationLanguagePreference.Spanish).Should().Be("es");
    }

    [Test]
    public void ExplicitLanguagePreferenceSurvivesDatabaseReopen()
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"language-{Guid.NewGuid():N}.db");
        try
        {
            using (var connection = new SQLite.SQLiteConnection(path))
            {
                PaliPractice.Services.Database.PracticeDatabaseMigrations.Apply(connection);
                var userData = new PaliPractice.Services.Database.Repositories.UserDataRepository(connection);
                userData.InitializeDefaultsIfNeeded();
                userData.SetSetting(SettingsKeys.AppearanceTranslationLanguage, (int)TranslationLanguagePreference.Spanish);
            }
            using (var connection = new SQLite.SQLiteConnection(path))
            {
                var userData = new PaliPractice.Services.Database.Repositories.UserDataRepository(connection);
                userData.InitializeDefaultsIfNeeded();
                userData.GetSetting(SettingsKeys.AppearanceTranslationLanguage, -1).Should().Be(2);
            }
        }
        finally { File.Delete(path); }
    }

}
