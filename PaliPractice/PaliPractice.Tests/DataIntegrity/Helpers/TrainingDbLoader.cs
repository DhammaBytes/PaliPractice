using Microsoft.Data.Sqlite;

namespace PaliPractice.Tests.DataIntegrity.Helpers;

/// <summary>
/// Represents a noun from training.db for integrity testing.
/// </summary>
public class TrainingNoun
{
    public int Id { get; set; }
    public int EbtCount { get; set; }
    public int LemmaId { get; set; }
    public string Lemma { get; set; } = string.Empty;
    public int Gender { get; set; }
    public string Stem { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty;
}

/// <summary>
/// Represents noun details from training.db for integrity testing.
/// </summary>
public class TrainingNounDetails
{
    public int Id { get; set; }
    public int LemmaId { get; set; }
    public string Variant { get; set; } = string.Empty;
    public string Meaning { get; set; } = string.Empty;
    public string Source1 { get; set; } = string.Empty;
    public string Sutta1 { get; set; } = string.Empty;
    public string Example1 { get; set; } = string.Empty;
    public string Source2 { get; set; } = string.Empty;
    public string Sutta2 { get; set; } = string.Empty;
    public string Example2 { get; set; } = string.Empty;
}

/// <summary>
/// Represents a verb from training.db for integrity testing.
/// </summary>
public class TrainingVerb
{
    public int Id { get; set; }
    public int EbtCount { get; set; }
    public int LemmaId { get; set; }
    public string Lemma { get; set; } = string.Empty;
    public string Stem { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty;
}

/// <summary>
/// Represents verb details from training.db for integrity testing.
/// </summary>
public class TrainingVerbDetails
{
    public int Id { get; set; }
    public int LemmaId { get; set; }
    public string Variant { get; set; } = string.Empty;
    public string VerbType { get; set; } = string.Empty;
    public string Trans { get; set; } = string.Empty;
    public string Meaning { get; set; } = string.Empty;
    public string Source1 { get; set; } = string.Empty;
    public string Sutta1 { get; set; } = string.Empty;
    public string Example1 { get; set; } = string.Empty;
    public string Source2 { get; set; } = string.Empty;
    public string Sutta2 { get; set; } = string.Empty;
    public string Example2 { get; set; } = string.Empty;
}

/// <summary>
/// Helper class for loading data from training.db for integrity testing.
/// </summary>
public class TrainingDbLoader : IDisposable
{
    readonly SqliteConnection _connection;
    bool _disposed;

    public const string DefaultTrainingDbPath =
        "/Users/ivm/Sources/PaliPractice/PaliPractice/PaliPractice/Data/training.db";

    public TrainingDbLoader(string? trainingDbPath = null)
    {
        var path = trainingDbPath ?? DefaultTrainingDbPath;
        _connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly");
        _connection.Open();
    }

    /// <summary>
    /// Load all nouns from training.db.
    /// </summary>
    public List<TrainingNoun> GetAllNouns()
    {
        var nouns = new List<TrainingNoun>();

        const string sql = "SELECT id, ebt_count, lemma_id, lemma, gender, stem, pattern FROM nouns";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            nouns.Add(new TrainingNoun
            {
                Id = reader.GetInt32(0),
                EbtCount = reader.GetInt32(1),
                LemmaId = reader.GetInt32(2),
                Lemma = reader.GetString(3),
                Gender = reader.GetInt32(4),
                Stem = reader.IsDBNull(5) ? "" : reader.GetString(5),
                Pattern = reader.GetString(6)
            });
        }

        return nouns;
    }

    /// <summary>
    /// Load all noun details from training.db.
    /// </summary>
    public List<TrainingNounDetails> GetAllNounDetails()
    {
        var details = new List<TrainingNounDetails>();

        const string sql = """
            SELECT id, lemma_id, word, meaning, source_1, sutta_1, example_1,
                   source_2, sutta_2, example_2
            FROM nouns_details
            """;

        using var command = _connection.CreateCommand();
        command.CommandText = sql;

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            details.Add(new TrainingNounDetails
            {
                Id = reader.GetInt32(0),
                LemmaId = reader.GetInt32(1),
                Variant = reader.IsDBNull(2) ? "" : reader.GetString(2),
                Meaning = reader.IsDBNull(3) ? "" : reader.GetString(3),
                Source1 = reader.IsDBNull(4) ? "" : reader.GetString(4),
                Sutta1 = reader.IsDBNull(5) ? "" : reader.GetString(5),
                Example1 = reader.IsDBNull(6) ? "" : reader.GetString(6),
                Source2 = reader.IsDBNull(7) ? "" : reader.GetString(7),
                Sutta2 = reader.IsDBNull(8) ? "" : reader.GetString(8),
                Example2 = reader.IsDBNull(9) ? "" : reader.GetString(9)
            });
        }

        return details;
    }

    /// <summary>
    /// Load all verbs from training.db.
    /// </summary>
    public List<TrainingVerb> GetAllVerbs()
    {
        var verbs = new List<TrainingVerb>();

        const string sql = "SELECT id, ebt_count, lemma_id, lemma, stem, pattern FROM verbs";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            verbs.Add(new TrainingVerb
            {
                Id = reader.GetInt32(0),
                EbtCount = reader.GetInt32(1),
                LemmaId = reader.GetInt32(2),
                Lemma = reader.GetString(3),
                Stem = reader.IsDBNull(4) ? "" : reader.GetString(4),
                Pattern = reader.GetString(5)
            });
        }

        return verbs;
    }

    /// <summary>
    /// Load all verb details from training.db.
    /// </summary>
    public List<TrainingVerbDetails> GetAllVerbDetails()
    {
        var details = new List<TrainingVerbDetails>();

        const string sql = """
            SELECT id, lemma_id, word, type, trans, meaning,
                   source_1, sutta_1, example_1, source_2, sutta_2, example_2
            FROM verbs_details
            """;

        using var command = _connection.CreateCommand();
        command.CommandText = sql;

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            details.Add(new TrainingVerbDetails
            {
                Id = reader.GetInt32(0),
                LemmaId = reader.GetInt32(1),
                Variant = reader.IsDBNull(2) ? "" : reader.GetString(2),
                VerbType = reader.IsDBNull(3) ? "" : reader.GetString(3),
                Trans = reader.IsDBNull(4) ? "" : reader.GetString(4),
                Meaning = reader.IsDBNull(5) ? "" : reader.GetString(5),
                Source1 = reader.IsDBNull(6) ? "" : reader.GetString(6),
                Sutta1 = reader.IsDBNull(7) ? "" : reader.GetString(7),
                Example1 = reader.IsDBNull(8) ? "" : reader.GetString(8),
                Source2 = reader.IsDBNull(9) ? "" : reader.GetString(9),
                Sutta2 = reader.IsDBNull(10) ? "" : reader.GetString(10),
                Example2 = reader.IsDBNull(11) ? "" : reader.GetString(11)
            });
        }

        return details;
    }

    /// <summary>
    /// Load all corpus declension form_ids for validation.
    /// </summary>
    public HashSet<long> GetCorpusDeclensionFormIds()
    {
        var formIds = new HashSet<long>();

        const string sql = "SELECT form_id FROM nouns_corpus_forms";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            formIds.Add(reader.GetInt64(0));
        }

        return formIds;
    }

    /// <summary>
    /// Load all corpus conjugation form_ids for validation.
    /// </summary>
    public HashSet<long> GetCorpusConjugationFormIds()
    {
        var formIds = new HashSet<long>();

        const string sql = "SELECT form_id FROM verbs_corpus_forms";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            formIds.Add(reader.GetInt64(0));
        }

        return formIds;
    }

    /// <summary>
    /// Load all non-reflexive verb lemma_ids.
    /// </summary>
    public HashSet<int> GetNonReflexiveLemmaIds()
    {
        var lemmaIds = new HashSet<int>();

        const string sql = "SELECT lemma_id FROM verbs_nonreflexive";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            lemmaIds.Add(reader.GetInt32(0));
        }

        return lemmaIds;
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
