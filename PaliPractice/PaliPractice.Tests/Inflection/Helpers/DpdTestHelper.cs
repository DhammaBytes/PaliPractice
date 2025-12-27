using Microsoft.Data.Sqlite;

namespace PaliPractice.Tests.Inflection.Helpers;

/// <summary>
/// Helper class for querying test data from the DPD database.
/// </summary>
public class DpdTestHelper : IDisposable
{
    readonly SqliteConnection _connection;
    bool _disposed;

    public DpdTestHelper(string dpdDbPath)
    {
        _connection = new SqliteConnection($"Data Source={dpdDbPath};Mode=ReadOnly");
        _connection.Open();
    }

    /// <summary>
    /// Represents a word from the DPD database with inflection HTML.
    /// </summary>
    public class DpdWord
    {
        public int Id { get; set; }
        public string Lemma1 { get; set; } = string.Empty;
        public string Lemma2 { get; set; } = string.Empty;
        public string Pattern { get; set; } = string.Empty;
        public string Pos { get; set; } = string.Empty;
        public int EbtCount { get; set; }
        public string InflectionsHtml { get; set; } = string.Empty;
    }

    /// <summary>
    /// Get top N words for a given pattern, ensuring distinct lemma_2 values.
    /// Orders by ebt_count descending.
    /// </summary>
    public List<DpdWord> GetTopWordsByPattern(string pattern, int limit = 3)
    {
        var words = new List<DpdWord>();

        // Use GROUP BY lemma_2 to get distinct entries, taking the one with highest ebt_count
        const string sql = """

                                       SELECT id, lemma_1, lemma_2, pattern, pos, ebt_count, inflections_html
                                       FROM dpd_headwords
                                       WHERE pattern = @pattern
                                       GROUP BY lemma_2
                                       ORDER BY MAX(ebt_count) DESC
                                       LIMIT @limit
                           """;

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@pattern", pattern);
        command.Parameters.AddWithValue("@limit", limit);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            words.Add(new DpdWord
            {
                Id = reader.GetInt32(0),
                Lemma1 = reader.GetString(1),
                Lemma2 = reader.GetString(2),
                Pattern = reader.GetString(3),
                Pos = reader.GetString(4),
                EbtCount = reader.GetInt32(5),
                InflectionsHtml = reader.GetString(6)
            });
        }

        return words;
    }

    /// <summary>
    /// Get all noun patterns from the database.
    /// </summary>
    public List<string> GetAllNounPatterns()
    {
        var patterns = new List<string>();

        const string sql = """

                                       SELECT DISTINCT pattern
                                       FROM dpd_headwords
                                       WHERE pos = 'noun'
                                       AND pattern != ''
                                       ORDER BY pattern
                           """;

        using var command = _connection.CreateCommand();
        command.CommandText = sql;

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            patterns.Add(reader.GetString(0));
        }

        return patterns;
    }

    /// <summary>
    /// Get all verb patterns from the database.
    /// </summary>
    public List<string> GetAllVerbPatterns()
    {
        var patterns = new List<string>();

        const string sql = """

                                       SELECT DISTINCT pattern
                                       FROM dpd_headwords
                                       WHERE pos = 'verb'
                                       AND pattern != ''
                                       ORDER BY pattern
                           """;

        using var command = _connection.CreateCommand();
        command.CommandText = sql;

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            patterns.Add(reader.GetString(0));
        }

        return patterns;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _connection?.Dispose();
            _disposed = true;
        }
    }
}
