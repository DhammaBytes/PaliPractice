using PaliPractice.Models.Words;
using PaliPractice.Services.Database.Entities;
using PaliPractice.Services.Database.Repositories;
using PaliPractice.Services.UserData;
using SQLite;

namespace PaliPractice.Tests.UserData;

[TestFixture]
public class MeaningLoaderTests
{
    SQLiteConnection _connection = null!;
    readonly List<string> _queries = [];
    string _language = "en";

    [SetUp]
    public void SetUp()
    {
        _connection = new SQLiteConnection(":memory:");
        _connection.CreateTable<Noun>();
        _connection.CreateTable<NounDetails>();
        _connection.CreateTable<NounCorpusForm>();
        _connection.CreateTable<NounIrregularForm>();
        _connection.Execute("ALTER TABLE nouns_details ADD COLUMN meaning TEXT");
        _connection.Execute("ALTER TABLE nouns_details ADD COLUMN meaning_ru TEXT");
        for (var id = 1; id <= 3; id++)
        {
            _connection.Insert(new Noun { Id = id, LemmaId = 10001, Lemma = "dhamma", Stem = "dhamm",
                Gender = Gender.Masculine, RawPattern = "a masc", PracticePrimary = id == 1 });
            _connection.Insert(new NounDetails { Id = id, LemmaId = 10001, Example1 = "unchanged example" });
            _connection.Execute("UPDATE nouns_details SET meaning=?, meaning_ru=? WHERE id=?", $"English {id}", $"Legacy Russian {id}", id);
        }
        _queries.Clear();
        _language = "en";
        _connection.Trace = true;
        _connection.Tracer = text => { if (text.Contains("SELECT", StringComparison.Ordinal)) _queries.Add(text); };
    }

    [TearDown]
    public void TearDown() => _connection.Dispose();

    void AddTranslations()
    {
        _connection.Execute("CREATE TABLE localized_meanings(headword_id INTEGER, language TEXT, meaning TEXT, PRIMARY KEY(headword_id,language))");
        foreach (var language in new[] { "ru", "es" })
            for (var id = 1; id <= 3; id++)
                _connection.Execute("INSERT INTO localized_meanings VALUES (?, ?, ?)", id, language, $"{language} {id}");
    }

    NounRepository Repository() => new(_connection, () => _language);
    static string[] Meanings(ILemma lemma) => lemma.Words.Select(word => word.Details!.Meaning).ToArray();
    string[] MeaningQueries() => _queries.Where(sql => sql.Contains(" AS meaning", StringComparison.Ordinal) || sql.Contains("localized_meanings WHERE", StringComparison.Ordinal)).ToArray();

    [TestCase("ru")]
    [TestCase("es")]
    public void CompleteSelectedLanguageDoesNotQueryEnglishAndCachesOnlyResolvedText(string language)
    {
        AddTranslations();
        var repository = Repository();
        var lemma = repository.GetLemma(10001)!;
        _language = language;
        _queries.Clear();
        repository.EnsureDetails(lemma);
        Meanings(lemma).Should().Equal($"{language} 1", $"{language} 2", $"{language} 3");
        MeaningQueries().Should().ContainSingle().Which.Should().Contain("localized_meanings");
        _queries.Should().NotContain(sql => sql.Contains("SELECT * FROM nouns_details", StringComparison.Ordinal));
        _queries.Clear();
        repository.EnsureDetails(lemma);
        _queries.Should().BeEmpty();
    }

    [Test]
    public void FallbackQueriesOnlyMissingAndBlankSenses()
    {
        AddTranslations();
        _connection.Execute("DELETE FROM localized_meanings WHERE headword_id=2 AND language='es'");
        _connection.Execute("UPDATE localized_meanings SET meaning='  ' WHERE headword_id=3 AND language='es'");
        _language = "es";
        var repository = Repository();
        var lemma = repository.GetLemma(10001)!;
        _queries.Clear();
        repository.EnsureDetails(lemma);
        Meanings(lemma).Should().Equal("es 1", "English 2", "English 3");
        var fallback = MeaningQueries().Single(sql => sql.Contains("FROM nouns_details", StringComparison.Ordinal));
        fallback.Should().Contain("IN (?,?)");
        lemma.Words[0].Details!.MeaningLanguage.Should().Be(TranslationLanguagePreference.Spanish);
        lemma.Words[1].Details!.MeaningLanguage.Should().Be(TranslationLanguagePreference.English);
    }

    [Test]
    public void RepeatedLanguageSwitchesClearMeaningsButKeepNeutralDetailsAndIdentity()
    {
        AddTranslations();
        var repository = Repository();
        var lemma = repository.GetLemma(10001)!;
        var ids = lemma.Words.Select(word => word.Id).ToArray();
        repository.EnsureDetails(lemma);
        var neutralDetails = lemma.Primary.Details;
        foreach (var language in new[] { "ru", "es", "en", "ru", "es" })
        {
            _language = language;
            repository.ClearMeaningCache();
            lemma.MeaningsLanguage.Should().BeNull();
            Meanings(lemma).Should().OnlyContain(text => text.Length == 0);
            lemma.Primary.Details.Should().BeSameAs(neutralDetails);
            repository.EnsureDetails(lemma);
            Meanings(lemma).Should().Equal(language == "en" ? ["English 1", "English 2", "English 3"] : [$"{language} 1", $"{language} 2", $"{language} 3"]);
            lemma.Primary.Details!.Example1.Should().Be("unchanged example");
            lemma.Words.Select(word => word.Id).Should().Equal(ids);
        }
    }

    [Test]
    public void LegacyBundleLoadsOnlyRussianColumnAndSpanishFallsBackToEnglish()
    {
        var repository = Repository();
        var lemma = repository.GetLemma(10001)!;
        _language = "ru";
        _queries.Clear();
        repository.EnsureDetails(lemma);
        Meanings(lemma).Should().Equal("Legacy Russian 1", "Legacy Russian 2", "Legacy Russian 3");
        MeaningQueries().Should().ContainSingle().Which.Should().Contain("meaning_ru AS meaning");
        _language = "es";
        _queries.Clear();
        repository.EnsureDetails(lemma);
        Meanings(lemma).Should().Equal("English 1", "English 2", "English 3");
        MeaningQueries().Should().ContainSingle().Which.Should().Contain("meaning AS meaning");
    }

    [TestCase("en")]
    [TestCase("de")]
    [TestCase("esoteric")]
    public void EnglishAndUnsupportedCodesUseOnlyEnglish(string language)
    {
        AddTranslations();
        var repository = Repository();
        var lemma = repository.GetLemma(10001)!;
        _language = language;
        _queries.Clear();
        repository.EnsureDetails(lemma);
        Meanings(lemma).Should().Equal("English 1", "English 2", "English 3");
        MeaningQueries().Should().ContainSingle().Which.Should().Contain("meaning AS meaning");
    }
    [Test]
    public async Task CacheClearCannotBeOverwrittenByAnOlderInFlightLoad()
    {
        AddTranslations();
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        _language = "ru";
        var repository = new NounRepository(_connection, () =>
        {
            var captured = _language;
            entered.Set();
            if (!release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException();
            return captured;
        });
        var lemma = repository.GetLemma(10001)!;
        var loading = Task.Run(() => repository.EnsureDetails(lemma));
        try
        {
            entered.Wait(TimeSpan.FromSeconds(10)).Should().BeTrue();
            _language = "es";
            var clearing = Task.Run(repository.ClearMeaningCache);
            release.Set();
            await Task.WhenAll(loading, clearing);
            Meanings(lemma).Should().OnlyContain(text => text.Length == 0);
            repository.EnsureDetails(lemma);
            Meanings(lemma).Should().Equal("es 1", "es 2", "es 3");
        }
        finally { release.Set(); await loading; }
    }
}
