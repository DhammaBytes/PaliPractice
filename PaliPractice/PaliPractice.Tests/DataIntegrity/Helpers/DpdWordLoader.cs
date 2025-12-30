using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace PaliPractice.Tests.DataIntegrity.Helpers;

/// <summary>
/// Represents a headword from the DPD database with all columns needed for integrity testing.
/// </summary>
public class DpdHeadword
{
    public int Id { get; set; }
    public string Lemma1 { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty;
    public string Pos { get; set; } = string.Empty;
    public string Stem { get; set; } = string.Empty;
    public int EbtCount { get; set; }
    public string Meaning1 { get; set; } = string.Empty;
    public string Source1 { get; set; } = string.Empty;
    public string Sutta1 { get; set; } = string.Empty;
    public string Example1 { get; set; } = string.Empty;
    public string Source2 { get; set; } = string.Empty;
    public string Sutta2 { get; set; } = string.Empty;
    public string Example2 { get; set; } = string.Empty;
    public string InflectionsHtml { get; set; } = string.Empty;
    public string Verb { get; set; } = string.Empty;
    public string Trans { get; set; } = string.Empty;

    /// <summary>
    /// Computed lemma_clean matching Python: re.sub(r" \d.*$", "", lemma_1)
    /// Removes trailing " 1", " 2.1", etc.
    /// </summary>
    public string LemmaClean => Regex.Replace(Lemma1, @" \d.*$", "");

    /// <summary>
    /// Computed clean stem matching Python: re.sub(r"[!*]", "", stem)
    /// Removes DPD marker characters.
    /// </summary>
    public string StemClean => Regex.Replace(Stem, @"[!*]", "");
}

/// <summary>
/// Extended DPD database helper that loads ALL columns needed for integrity testing.
/// </summary>
public class DpdWordLoader : IDisposable
{
    readonly SqliteConnection _connection;
    bool _disposed;

    public DpdWordLoader(string? dpdDbPath = null)
    {
        var path = dpdDbPath ?? TestPaths.DpdDbPath;
        _connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly");
        _connection.Open();
    }

    /// <summary>
    /// Load all headwords from DPD indexed by ID for O(1) lookup.
    /// </summary>
    public Dictionary<int, DpdHeadword> GetAllHeadwordsById()
    {
        var result = new Dictionary<int, DpdHeadword>();

        const string sql = """
            SELECT
                id, lemma_1, pattern, pos, stem, ebt_count,
                meaning_1, source_1, sutta_1, example_1,
                source_2, sutta_2, example_2,
                inflections_html, verb, trans
            FROM dpd_headwords
            """;

        using var command = _connection.CreateCommand();
        command.CommandText = sql;

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var headword = new DpdHeadword
            {
                Id = reader.GetInt32(0),
                Lemma1 = reader.IsDBNull(1) ? "" : reader.GetString(1),
                Pattern = reader.IsDBNull(2) ? "" : reader.GetString(2),
                Pos = reader.IsDBNull(3) ? "" : reader.GetString(3),
                Stem = reader.IsDBNull(4) ? "" : reader.GetString(4),
                EbtCount = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                Meaning1 = reader.IsDBNull(6) ? "" : reader.GetString(6),
                Source1 = reader.IsDBNull(7) ? "" : reader.GetString(7),
                Sutta1 = reader.IsDBNull(8) ? "" : reader.GetString(8),
                Example1 = reader.IsDBNull(9) ? "" : reader.GetString(9),
                Source2 = reader.IsDBNull(10) ? "" : reader.GetString(10),
                Sutta2 = reader.IsDBNull(11) ? "" : reader.GetString(11),
                Example2 = reader.IsDBNull(12) ? "" : reader.GetString(12),
                InflectionsHtml = reader.IsDBNull(13) ? "" : reader.GetString(13),
                Verb = reader.IsDBNull(14) ? "" : reader.GetString(14),
                Trans = reader.IsDBNull(15) ? "" : reader.GetString(15)
            };
            result[headword.Id] = headword;
        }

        return result;
    }

    /// <summary>
    /// Get a single headword by ID.
    /// </summary>
    public DpdHeadword? GetHeadwordById(int id)
    {
        const string sql = """
            SELECT
                id, lemma_1, pattern, pos, stem, ebt_count,
                meaning_1, source_1, sutta_1, example_1,
                source_2, sutta_2, example_2,
                inflections_html, verb, trans
            FROM dpd_headwords
            WHERE id = @id
            """;

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@id", id);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new DpdHeadword
            {
                Id = reader.GetInt32(0),
                Lemma1 = reader.IsDBNull(1) ? "" : reader.GetString(1),
                Pattern = reader.IsDBNull(2) ? "" : reader.GetString(2),
                Pos = reader.IsDBNull(3) ? "" : reader.GetString(3),
                Stem = reader.IsDBNull(4) ? "" : reader.GetString(4),
                EbtCount = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                Meaning1 = reader.IsDBNull(6) ? "" : reader.GetString(6),
                Source1 = reader.IsDBNull(7) ? "" : reader.GetString(7),
                Sutta1 = reader.IsDBNull(8) ? "" : reader.GetString(8),
                Example1 = reader.IsDBNull(9) ? "" : reader.GetString(9),
                Source2 = reader.IsDBNull(10) ? "" : reader.GetString(10),
                Sutta2 = reader.IsDBNull(11) ? "" : reader.GetString(11),
                Example2 = reader.IsDBNull(12) ? "" : reader.GetString(12),
                InflectionsHtml = reader.IsDBNull(13) ? "" : reader.GetString(13),
                Verb = reader.IsDBNull(14) ? "" : reader.GetString(14),
                Trans = reader.IsDBNull(15) ? "" : reader.GetString(15)
            };
        }

        return null;
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
