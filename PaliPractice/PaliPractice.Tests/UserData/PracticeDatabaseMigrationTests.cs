using PaliPractice.Services.Database;
using PaliPractice.Presentation.Practice.ViewModels.Common;
using PaliPractice.Services.Database.Repositories;
using PaliPractice.Services.UserData;
using PaliPractice.Services.UserData.Entities;
using SQLite;
using Path = System.IO.Path;
using PaliPractice.Services.Practice;
using PaliPractice.Tests.Practice.Builders;
using PaliPractice.Tests.Practice.Fakes;

namespace PaliPractice.Tests.UserData;

[TestFixture]
public class PracticeDatabaseMigrationTests
{
    [TestCase(PracticeType.Declension)]
    [TestCase(PracticeType.Conjugation)]
    public void FiveHundredDormantRowsCannotHideAnEligibleReview(PracticeType type)
    {
        using var connection = new SQLiteConnection(":memory:");
        PracticeDatabaseMigrations.Apply(connection);
        var userData = new UserDataRepository(connection);
        userData.InitializeDefaultsIfNeeded();
        var fixture = (type == PracticeType.Declension ? TestScenarioBuilder.Small() : TestScenarioBuilder.VerbSmall()).Build();
        foreach (var setting in fixture.UserData.GetAllSettings())
            userData.SetSetting(setting.Key, setting.Value);
        var db = new RepositoryBackedTestDatabaseService(fixture.Nouns, fixture.Verbs, userData);
        var builder = new PracticeQueueBuilder(db);
        var eligible = builder.GetEligibleFormIds(type).First();
        var table = type == PracticeType.Declension ? "nouns_form_mastery" : "verbs_form_mastery";
        for (var index = 0; index < 501; index++)
        {
            var dormant = type == PracticeType.Declension
                ? (50000L + index) * 10000 + 1110
                : (90000L + index) * 100000 + 11110;
            connection.Execute($"INSERT INTO {table} (form_id, mastery_level, previous_level, last_practiced_utc) VALUES (?, 4, 4, ?)",
                index == 500 ? eligible : dormant, DateTime.UtcNow.AddYears(-2).AddDays(index).Ticks);
        }
        var stats = new StatisticsRepository(connection, userData, kind => builder.GetEligibleFormIds(kind).ToHashSet());
        var current = type == PracticeType.Declension ? stats.GetNounStats() : stats.GetVerbStats();
        current.DueForReview.Should().Be(1);
        builder.BuildQueue(type, 5).Should().Contain(item => item.FormId == eligible && item.Source == PracticeItemSource.DueForReview);
        connection.ExecuteScalar<int>($"SELECT COUNT(*) FROM {table}").Should().Be(501);
    }

    [Test]
    public void RemovedAtthiNounHistoryIsRecoveredFromReleasedGrammar()
    {
        using var connection = new SQLiteConnection(":memory:");
        connection.Execute("CREATE TABLE nouns_practice_history (id INTEGER PRIMARY KEY, form_id BIGINT, old_level INTEGER, new_level INTEGER, practiced_utc BIGINT)");
        connection.Execute("INSERT INTO nouns_practice_history VALUES (1, 100502110, 4, 5, 0)");
        PracticeDatabaseMigrations.Apply(connection);
        var recovered = connection.Table<NounsPracticeHistory>().Single();
        recovered.FormText.Should().Be("atthiṃ");
        recovered.LemmaText.Should().Be("atthi");
        recovered.GrammarText.Should().Be("Accusative/Masculine/Singular");
        recovered.SnapshotOrigin.Should().Be(HistorySnapshotOrigin.ReconstructedV11);
        // Later starts do not rewrite this reconstruction or its original mastery transition.
        PracticeDatabaseMigrations.Apply(connection);
        recovered.NewLevel.Should().Be(5);
    }

    [Test]
    public void DormantMasteryCountsTowardLifetimeButNotCurrentDueReview()
    {
        using var connection = new SQLiteConnection(":memory:");
        PracticeDatabaseMigrations.Apply(connection);
        var userData = new UserDataRepository(connection);
        foreach (var formId in new long[] { 123451110, 123461110 })
            connection.Insert(new NounsFormMastery
            {
                FormId = formId, MasteryLevel = 4, LastPracticedUtc = DateTime.UtcNow.AddYears(-1)
            });
        var eligible = new HashSet<long> { 123451110 };
        var statistics = new StatisticsRepository(connection, userData, _ => eligible);
        var stats = statistics.GetNounStats();
        stats.TotalPracticed.Should().Be(2);
        stats.Distribution.Learning.Should().Be(2);
        stats.DueForReview.Should().Be(1);
        eligible.Clear(); // Changed filters are reflected without deleting mastery.
        statistics.GetNounStats().DueForReview.Should().Be(0);
        userData.GetPracticedNounFormIds().Should().HaveCount(2);
    }

    [Test]
    public void LegacyHistoryAndDormantMasterySurviveIdempotentMigration()
    {
        using var connection = new SQLiteConnection(":memory:");
        connection.Execute("CREATE TABLE nouns_practice_history (id INTEGER PRIMARY KEY, form_id BIGINT, old_level INTEGER, new_level INTEGER, practiced_utc BIGINT)");
        connection.Execute("INSERT INTO nouns_practice_history VALUES (1, 123451110, 4, 5, 0)");
        connection.CreateTable<NounsFormMastery>();
        connection.Insert(new NounsFormMastery { FormId = 123451110, MasteryLevel = 5 });

        PracticeDatabaseMigrations.Apply(connection);
        PracticeDatabaseMigrations.Apply(connection);

        connection.ExecuteScalar<int>("PRAGMA user_version").Should().Be(1);
        var history = connection.Table<NounsPracticeHistory>().Single();
        history.FormId.Should().Be(123451110);
        history.NewLevel.Should().Be(5);
        string.IsNullOrEmpty(history.FormText).Should().BeTrue();
        connection.Table<NounsFormMastery>().Single().MasteryLevel.Should().Be(5);
    }

    [Test]
    public void FailedMigrationRollsBackSchemaAndVersion()
    {
        using var connection = new SQLiteConnection(":memory:");
        // An incompatible legacy table makes index creation fail after earlier tables were created.
        connection.Execute("CREATE TABLE nouns_form_mastery (form_id BIGINT PRIMARY KEY, mastery_level TEXT)");
        connection.Execute("CREATE TABLE idx_verbs_history_date (value INTEGER)");
        Action migrate = () => PracticeDatabaseMigrations.Apply(connection);
        migrate.Should().Throw<SQLiteException>();
        connection.ExecuteScalar<int>("PRAGMA user_version").Should().Be(0);
        connection.ExecuteScalar<int>("SELECT COUNT(*) FROM sqlite_master WHERE name = 'nouns_practice_history'")
            .Should().Be(0);
        connection.ExecuteScalar<int>("SELECT COUNT(*) FROM sqlite_master WHERE name = 'idx_verbs_history_date'")
            .Should().Be(1);
    }

    [Test]
    public void NewerSchemaIsRejectedWithoutChanges()
    {
        using var connection = new SQLiteConnection(":memory:");
        connection.Execute("PRAGMA user_version = 2");
        Action migrate = () => PracticeDatabaseMigrations.Apply(connection);
        migrate.Should().Throw<InvalidDataException>();
        connection.ExecuteScalar<int>("PRAGMA user_version").Should().Be(2);
        connection.ExecuteScalar<int>("SELECT COUNT(*) FROM sqlite_master").Should().Be(0);
    }

    [TestCase(PracticeType.Declension, "nouns_practice_history", "nouns_form_mastery")]
    [TestCase(PracticeType.Conjugation, "verbs_practice_history", "verbs_form_mastery")]
    public void FailedHistoryInsertCannotAdvanceMastery(PracticeType type, string history, string mastery)
    {
        using var connection = new SQLiteConnection(":memory:");
        PracticeDatabaseMigrations.Apply(connection);
        var repository = new UserDataRepository(connection);
        connection.Execute($"CREATE TRIGGER reject_history BEFORE INSERT ON {history} BEGIN SELECT RAISE(ABORT, 'injected failure'); END");
        Action record = () => repository.RecordPracticeResult(123451110, type, true,
            new PracticeSnapshot("rūpaṃ", "rūpa", "Accusative/Neuter/Singular"));
        record.Should().Throw<SQLiteException>();
        connection.ExecuteScalar<int>($"SELECT COUNT(*) FROM {mastery}").Should().Be(0);
        connection.ExecuteScalar<int>($"SELECT COUNT(*) FROM {history}").Should().Be(0);
    }

    [Test]
    public void SnapshotSurvivesReopenWithoutDictionaryAccess()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pali-history-{Guid.NewGuid():N}.db");
        try
        {
            using (var connection = new SQLiteConnection(path))
            {
                PracticeDatabaseMigrations.Apply(connection);
                new UserDataRepository(connection).RecordPracticeResult(123452310,
                    PracticeType.Declension, true,
                    PracticeSnapshot.Capture(123452310, PracticeType.Declension, "rūpaṃ", "rūpa"));
            }
            using var reopened = new SQLiteConnection(path);
            var history = reopened.Table<NounsPracticeHistory>().Single();
            history.FormText.Should().Be("rūpaṃ");
            history.LemmaText.Should().Be("rūpa");
            history.GrammarText.Should().Be("Accusative/Neuter/Singular");
            history.SnapshotOrigin.Should().Be(HistorySnapshotOrigin.Practiced);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
