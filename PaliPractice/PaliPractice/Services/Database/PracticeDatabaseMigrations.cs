using PaliPractice.Services.UserData.Entities;
using SQLite;

namespace PaliPractice.Services.Database;

/// <summary>Transactional, append-only schema upgrades for the user's practice database.</summary>
public static class PracticeDatabaseMigrations
{
    public const int CurrentVersion = 1;

    public static void Apply(SQLiteConnection connection)
    {
        var version = connection.ExecuteScalar<int>("PRAGMA user_version");
        if (version > CurrentVersion)
            throw new InvalidDataException($"Unsupported practice database version {version}");
        if (version == CurrentVersion)
            return;
        connection.RunInTransaction(() =>
        {
            CreateVersionOne(connection);
            LegacyHistorySnapshots.Backfill(connection);
            connection.Execute($"PRAGMA user_version = {CurrentVersion}");
        });
    }

    static void CreateVersionOne(SQLiteConnection connection)
    {
        // Create noun-specific tables
        connection.CreateTable<NounsFormMastery>();
        connection.CreateTable<NounsPracticeHistory>();

        // Create verb-specific tables
        connection.CreateTable<VerbsFormMastery>();
        connection.CreateTable<VerbsPracticeHistory>();

        // Create shared tables
        connection.CreateTable<UserSetting>();
        connection.CreateTable<DailyProgress>();

        // Create indices for efficient querying
        connection.Execute("CREATE INDEX IF NOT EXISTS idx_nouns_mastery_level ON nouns_form_mastery(mastery_level)");
        connection.Execute("CREATE INDEX IF NOT EXISTS idx_verbs_mastery_level ON verbs_form_mastery(mastery_level)");
        connection.Execute("CREATE INDEX IF NOT EXISTS idx_nouns_history_date ON nouns_practice_history(practiced_utc DESC)");
        connection.Execute("CREATE INDEX IF NOT EXISTS idx_verbs_history_date ON verbs_practice_history(practiced_utc DESC)");
    }
}
