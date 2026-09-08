using PaliPractice.Services.Database;
using PaliPractice.Services.UserData.Entities;
using SQLite;

namespace PaliPractice.Tests.UserData;

[TestFixture]
public class LegacyHistorySnapshotsTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void EmptyOrAlreadyCapturedHistoryDoesNotLoadResource(bool existing)
    {
        using var db = new SQLiteConnection(":memory:");
        db.CreateTable<NounsPracticeHistory>();
        db.CreateTable<VerbsPracticeHistory>();
        if (existing)
        {
            db.Insert(new NounsPracticeHistory { FormId = 1, FormText = "captured noun" });
            db.Insert(new VerbsPracticeHistory { FormId = 2, FormText = "captured verb" });
        }
        LegacyHistorySnapshots.Backfill(db, () => throw new AssertionException("Resource must remain unloaded"));
    }

    [Test]
    public void BatchesSkipUnknownRowsPreserveSnapshotsAndShareOneLoadAcrossTables()
    {
        using var db = new SQLiteConnection(":memory:");
        db.CreateTable<NounsPracticeHistory>();
        db.CreateTable<VerbsPracticeHistory>();
        for (int index = 0; index < 900; index++)
        {
            db.Insert(new NounsPracticeHistory
            {
                FormId = index < 300 ? 999 : 1,
                FormText = index >= 600 ? "captured" : "",
                NewLevel = 5,
                SnapshotOrigin = index >= 600 ? HistorySnapshotOrigin.Practiced : HistorySnapshotOrigin.Unknown
            });
        }
        db.Insert(new VerbsPracticeHistory { FormId = 2 });
        int loads = 0;
        Dictionary<long, string[]> Load()
        {
            loads++;
            return new() { [1] = ["noun", "lemma", "grammar"], [2] = ["verb", "lemma", "grammar"] };
        }
        LegacyHistorySnapshots.Backfill(db, Load);
        loads.Should().Be(1);
        db.ExecuteScalar<int>("SELECT COUNT(*) FROM nouns_practice_history WHERE form_text=''").Should().Be(300);
        db.ExecuteScalar<int>("SELECT COUNT(*) FROM nouns_practice_history WHERE form_text='noun' AND snapshot_origin=2").Should().Be(300);
        db.ExecuteScalar<int>("SELECT COUNT(*) FROM nouns_practice_history WHERE form_text='captured' AND snapshot_origin=1").Should().Be(300);
        db.ExecuteScalar<int>("SELECT COUNT(*) FROM nouns_practice_history WHERE new_level=5").Should().Be(900);
        db.Table<VerbsPracticeHistory>().Single().FormText.Should().Be("verb");

        // A separate migration call has its own resource lifetime; no static cache survives.
        LegacyHistorySnapshots.Backfill(db, Load);
        loads.Should().Be(2);
    }

    [Test]
    public void BackfillFailureRollsBackBothHistoriesAndVersion()
    {
        using var db = new SQLiteConnection(":memory:");
        db.CreateTable<NounsPracticeHistory>();
        db.CreateTable<VerbsPracticeHistory>();
        db.Insert(new NounsPracticeHistory { FormId = 100502110 });
        db.Insert(new VerbsPracticeHistory { FormId = 7000111110 });
        db.Execute("CREATE TRIGGER reject_history BEFORE UPDATE ON verbs_practice_history BEGIN SELECT RAISE(ABORT, 'injected failure'); END");
        Action migrate = () => PracticeDatabaseMigrations.Apply(db);
        migrate.Should().Throw<SQLiteException>();
        db.ExecuteScalar<int>("PRAGMA user_version").Should().Be(0);
        db.Table<NounsPracticeHistory>().Single().FormText.Should().BeEmpty();
        db.Table<VerbsPracticeHistory>().Single().FormText.Should().BeEmpty();
    }
}
