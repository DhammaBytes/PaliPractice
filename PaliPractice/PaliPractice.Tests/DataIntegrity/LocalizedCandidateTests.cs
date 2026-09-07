using PaliPractice.Models.Words;
using PaliPractice.Presentation.Practice.ViewModels.Common;
using PaliPractice.Services.Database.Repositories;
using PaliPractice.Services.Practice;
using PaliPractice.Services.UserData;
using PaliPractice.Tests.Practice.Fakes;
using SQLite;

namespace PaliPractice.Tests.DataIntegrity;

[TestFixture]
public class LocalizedCandidateTests
{
    [Test]
    public void EveryDisplayedSenseResolvesSelectedLanguageWithoutChangingEligibleIds()
    {
        using var connection = new SQLiteConnection(TestPaths.PaliDbPath, SQLiteOpenFlags.ReadOnly);
        var language = "en";
        var nouns = new NounRepository(connection, () => language);
        var verbs = new VerbRepository(connection, () => language);
        var queue = new PracticeQueueBuilder(new RepositoryBackedTestDatabaseService(nouns, verbs, new FakeUserDataRepository()));
        var beforeNouns = queue.GetEligibleFormIds(PracticeType.Declension).Order().ToArray();
        var beforeVerbs = queue.GetEligibleFormIds(PracticeType.Conjugation).Order().ToArray();
        foreach (var selected in new[] { "en", "ru", "es" })
        {
            language = selected;
            nouns.ClearMeaningCache();
            verbs.ClearMeaningCache();
            CheckRepository(connection, nouns, "nouns_details", selected);
            CheckRepository(connection, verbs, "verbs_details", selected);
            queue.GetEligibleFormIds(PracticeType.Declension).Order().Should().Equal(beforeNouns);
            queue.GetEligibleFormIds(PracticeType.Conjugation).Order().Should().Equal(beforeVerbs);
        }
    }

    static void CheckRepository(SQLiteConnection connection, ILemmaRepository repository, string table, string language)
    {
        var english = connection.Query<MeaningLoader.MeaningRow>($"SELECT id, meaning FROM {table}").ToDictionary(row => row.Id, row => row.Meaning);
        var hasLocalized = connection.ExecuteScalar<int>("SELECT count(*) FROM sqlite_master WHERE name='localized_meanings'") == 1;
        var localized = new Dictionary<int, string>();
        if (language != "en" && hasLocalized)
            localized = connection.Query<MeaningLoader.MeaningRow>("SELECT headword_id AS id, meaning FROM localized_meanings WHERE language=?", language).ToDictionary(row => row.Id, row => row.Meaning);
        else if (language == "ru")
            localized = connection.Query<MeaningLoader.MeaningRow>($"SELECT id, meaning_ru AS meaning FROM {table}").ToDictionary(row => row.Id, row => row.Meaning);
        var failures = new List<string>();
        var checkedSenses = 0;
        foreach (var lemma in repository.GetLemmasByRank(1, repository.GetCount()))
        {
            repository.EnsureDetails(lemma);
            foreach (var word in lemma.Words)
            {
                var translated = localized.TryGetValue(word.Id, out var text) && !string.IsNullOrWhiteSpace(text);
                var expected = translated ? text : english[word.Id];
                var actualLanguage = translated ? TranslationLanguageResolver.PreferenceFromLanguageCode(language) : TranslationLanguagePreference.English;
                if (word.Details?.Meaning != expected || word.Details.MeaningLanguage != actualLanguage)
                    failures.Add($"{table}/{word.Id}/{language}");
                checkedSenses++;
            }
        }
        checkedSenses.Should().BeGreaterThan(0);
        failures.Should().BeEmpty("all displayed senses must resolve their pinned translation or English fallback");
    }
}
