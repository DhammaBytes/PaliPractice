using PaliPractice.Models.Words;
using PaliPractice.Presentation.Practice.ViewModels.Common;
using PaliPractice.Services.Database;
using PaliPractice.Services.Database.Providers;
using PaliPractice.Services.Database.Repositories;
using PaliPractice.Services.Practice;
using PaliPractice.Services.UserData;
using SQLite;

namespace PaliPractice.Tests.DataIntegrity;

[TestFixture]
public class MultilingualProvisioningTests
{
    [TestCase(true, false)]
    [TestCase(false, false)]
    [TestCase(false, true)]
    public void FreshAndUpgradedServicePreservesUserDataAcrossLanguageChanges(bool direct, bool upgrade)
    {
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"pali-provision-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            if (upgrade) SeedLegacyData(directory);
            var service = new DatabaseService(new FilesProvider(directory, direct));
            service.HasFatalFailure.Should().BeFalse();
            service.PreloadCaches();
            var queue = new PracticeQueueBuilder(service);
            var nounIds = queue.GetEligibleFormIds(PracticeType.Declension).Order().ToArray();
            var verbIds = queue.GetEligibleFormIds(PracticeType.Conjugation).Order().ToArray();
            nounIds.Should().NotBeEmpty().And.NotContain(100502110);
            verbIds.Should().NotBeEmpty();
            foreach (var language in new[] { TranslationLanguagePreference.Spanish, TranslationLanguagePreference.Russian, TranslationLanguagePreference.English })
            {
                service.UserData.SetSetting(SettingsKeys.AppearanceTranslationLanguage, (int)language);
                service.Nouns.ClearMeaningCache();
                service.Verbs.ClearMeaningCache();
                CheckMeaning(service.Nouns, language);
                CheckMeaning(service.Verbs, language);
                queue.GetEligibleFormIds(PracticeType.Declension).Order().Should().Equal(nounIds);
                queue.GetEligibleFormIds(PracticeType.Conjugation).Order().Should().Equal(verbIds);
            }
            if (upgrade)
            {
                service.UserData.GetNounFormMastery(100502110)!.MasteryLevel.Should().Be(5);
                var history = service.UserData.GetRecentNounHistory().Single();
                history.FormText.Should().Be("atthiṃ");
                history.LemmaText.Should().Be("atthi");
                service.Statistics.GetNounStats().DueForReview.Should().Be(0);
            }
            var reopened = new DatabaseService(new FilesProvider(directory, direct));
            reopened.HasFatalFailure.Should().BeFalse();
            reopened.UserData.GetSetting(SettingsKeys.AppearanceTranslationLanguage, -1).Should().Be(0);
            if (upgrade) reopened.UserData.GetRecentNounHistory().Single().FormText.Should().Be("atthiṃ");
        }
        finally { Directory.Delete(directory, true); }
    }

    static void CheckMeaning(ILemmaRepository repository, TranslationLanguagePreference language)
    {
        var lemma = repository.GetLemmasByRank(1, 1).Single();
        repository.EnsureDetails(lemma);
        lemma.MeaningsLanguage.Should().Be(language);
        lemma.Words.Select(word => word.Details!.Meaning).Should().OnlyContain(text => !string.IsNullOrWhiteSpace(text));
    }

    static void SeedLegacyData(string directory)
    {
        using var dictionary = new SQLiteConnection(System.IO.Path.Combine(directory, "pali.db"));
        dictionary.Execute("PRAGMA user_version=1");
        using var practice = new SQLiteConnection(System.IO.Path.Combine(directory, "practice.db"));
        practice.Execute("CREATE TABLE nouns_form_mastery (form_id BIGINT PRIMARY KEY, mastery_level INTEGER, previous_level INTEGER, last_practiced_utc BIGINT)");
        practice.Execute("INSERT INTO nouns_form_mastery VALUES (100502110, 5, 4, 0)");
        practice.Execute("CREATE TABLE nouns_practice_history (id INTEGER PRIMARY KEY, form_id BIGINT, old_level INTEGER, new_level INTEGER, practiced_utc BIGINT)");
        practice.Execute("INSERT INTO nouns_practice_history VALUES (1, 100502110, 4, 5, 0)");
    }

    sealed class FilesProvider(string directory, bool direct) : IBundledFileProvider
    {
        static string Source(string relative) => System.IO.Path.Combine(System.IO.Path.GetDirectoryName(TestPaths.PaliDbPath)!, System.IO.Path.GetFileName(relative));
        public string? TryGetReadOnlyPath(string relativePath) => direct ? Source(relativePath) : null;
        public Task<Stream> OpenReadStreamAsync(string relativePath) => Task.FromResult<Stream>(File.OpenRead(Source(relativePath)));
        public string GetUserDataDirectory() => directory;
    }
}
