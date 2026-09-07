using SQLite;

namespace PaliPractice.Services.Database;

/// <summary>Validate a complete replacement before publishing it to copied-asset consumers.</summary>
internal static class BundledDatabaseCopy
{
    public static void Validate(string path, int expectedVersion)
    {
        using var connection = new SQLiteConnection(path, SQLiteOpenFlags.ReadOnly);
        if (connection.ExecuteScalar<string>("PRAGMA integrity_check") != "ok")
            throw new InvalidDataException("Bundled database integrity check failed");
        if (connection.ExecuteScalar<int>("PRAGMA user_version") != expectedVersion)
            throw new InvalidDataException("Bundled database version differs from its version file");
        foreach (var table in new[] { "nouns", "verbs", "nouns_details", "verbs_details" })
            if (connection.ExecuteScalar<int>("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=?", table) != 1)
                throw new InvalidDataException($"Bundled database is missing {table}");
    }

    public static void Replace(Stream source, string destination, int expectedVersion)
    {
        var temporary = destination + ".tmp";
        try
        {
            using (var output = new FileStream(temporary, FileMode.Create, FileAccess.Write))
            {
                source.CopyTo(output);
                output.Flush(flushToDisk: true);
            }
            Validate(temporary, expectedVersion);
            File.Move(temporary, destination, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
        }
    }
}
