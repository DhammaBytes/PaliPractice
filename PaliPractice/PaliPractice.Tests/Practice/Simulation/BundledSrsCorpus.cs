using PaliPractice.Presentation.Practice.ViewModels.Common;
using PaliPractice.Services.Database.Repositories;
using SQLite;

namespace PaliPractice.Tests.Practice.Simulation;

/// <summary>Read-only repositories with a separate SQL oracle for the default practice sets.</summary>
internal sealed class BundledSrsCorpus : IDisposable
{
    readonly SQLiteConnection _connection = new(TestPaths.PaliDbPath, SQLiteOpenFlags.ReadOnly);
    public NounRepository Nouns { get; }
    public VerbRepository Verbs { get; }

    public BundledSrsCorpus()
    {
        Nouns = new NounRepository(_connection);
        Verbs = new VerbRepository(_connection);
    }

    public HashSet<long> DefaultEligible(PracticeType type)
    {
        // Decode stored IDs independently. Do not call queue eligibility or
        // repository HasAttestedForm to decide which IDs should be available.
        var sql = type == PracticeType.Declension ? """
            WITH ranked AS (
                SELECT id, gender, ROW_NUMBER() OVER (ORDER BY ebt_count DESC, lemma_id) AS rank
                FROM nouns WHERE practice_primary = 1
            )
            SELECT DISTINCT f.form_id / 10 * 10 AS FormId
            FROM nouns_corpus_forms f JOIN ranked n ON n.id = f.headword_id
            WHERE n.rank <= 100 AND f.form_id % 10 BETWEEN 1 AND 6
              AND f.form_id % 10000 / 1000 IN (1, 2)
              AND f.form_id % 1000 / 100 = n.gender
              AND f.form_id % 100 / 10 IN (1, 2)
            """ : """
            WITH ranked AS (
                SELECT id, ROW_NUMBER() OVER (ORDER BY ebt_count DESC, lemma_id) AS rank
                FROM verbs WHERE practice_primary = 1
            )
            SELECT DISTINCT f.form_id / 10 * 10 AS FormId
            FROM verbs_corpus_forms f JOIN ranked v ON v.id = f.headword_id
            WHERE v.rank <= 100 AND f.form_id % 10 BETWEEN 1 AND 7
              AND f.form_id % 100000 / 10000 = 1
              AND f.form_id % 10000 / 1000 IN (1, 2, 3)
              AND f.form_id % 1000 / 100 IN (1, 2)
              AND f.form_id % 100 / 10 = 1
              AND NOT (f.form_id % 10000 / 1000 = 3 AND f.form_id % 1000 / 100 = 1)
            """;
        return _connection.Query<FormKey>(sql).Select(f => f.FormId).ToHashSet();
    }

    public void Dispose() => _connection.Dispose();

    public sealed class FormKey
    {
        public long FormId { get; set; }
    }
}
