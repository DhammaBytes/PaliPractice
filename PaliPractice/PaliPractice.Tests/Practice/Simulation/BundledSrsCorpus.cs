using PaliPractice.Presentation.Practice.ViewModels.Common;
using PaliPractice.Services.Database.Repositories;
using SQLite;

namespace PaliPractice.Tests.Practice.Simulation;

/// <summary>Read-only repositories with a separate oracle over raw primary corpus rows.</summary>
internal sealed class BundledSrsCorpus : IDisposable
{
    readonly SQLiteConnection _connection = new(TestPaths.PaliDbPath, SQLiteOpenFlags.ReadOnly);
    readonly List<CorpusCard> _nouns;
    readonly List<CorpusCard> _verbs;
    public NounRepository Nouns { get; }
    public VerbRepository Verbs { get; }

    public BundledSrsCorpus()
    {
        Nouns = new NounRepository(_connection);
        Verbs = new VerbRepository(_connection);
        _nouns = ReadCards("nouns", "gender");
        _verbs = ReadCards("verbs", "0");
    }

    List<CorpusCard> ReadCards(string table, string gender) => _connection.Query<CorpusCard>($"""
        WITH ranked AS (
            SELECT id, {gender} AS Gender, pattern,
                   ROW_NUMBER() OVER (ORDER BY ebt_count DESC, lemma_id) AS Rank
            FROM {table} WHERE practice_primary = 1
        )
        SELECT DISTINCT f.form_id / 10 * 10 AS FormId, n.Rank, n.Gender, n.pattern AS Pattern
        FROM {table}_corpus_forms f JOIN ranked n ON n.id = f.headword_id
        WHERE f.form_id % 10 BETWEEN 1 AND {(table == "nouns" ? 6 : 7)}
        """);

    public HashSet<long> DefaultEligible(PracticeType type) => Eligible(type, SrsFilter.Default(type));

    public HashSet<long> Eligible(PracticeType type, SrsFilter filter) =>
        (type == PracticeType.Declension ? _nouns : _verbs)
        .Where(c => c.Rank >= filter.MinRank && c.Rank <= filter.MaxRank)
        .Where(c => filter.RawPatterns == null || filter.RawPatterns.Contains(c.Pattern))
        .Where(c => type == PracticeType.Declension ? NounMatches(c, filter) : VerbMatches(c, filter))
        .Select(c => c.FormId).ToHashSet();

    // These decode the identity contract directly, without queue eligibility,
    // HasAttestedForm, or production parent-pattern mapping helpers.
    static bool NounMatches(CorpusCard c, SrsFilter f)
    {
        var genderEnabled = c.Gender switch
        {
            1 => f.Masculine.Length > 0, 2 => f.Feminine.Length > 0,
            3 => f.Neuter.Length > 0, _ => false
        };
        return genderEnabled && f.Categories.Contains((int)(c.FormId % 10000 / 1000)) &&
        c.FormId % 1000 / 100 == c.Gender &&
        f.Numbers.Contains((int)(c.FormId % 100 / 10));
    }

    static bool VerbMatches(CorpusCard c, SrsFilter f) =>
        f.Categories.Contains((int)(c.FormId % 100000 / 10000)) &&
        f.Persons.Contains((int)(c.FormId % 10000 / 1000)) &&
        f.Numbers.Contains((int)(c.FormId % 1000 / 100)) &&
        f.Voices.Contains((int)(c.FormId % 100 / 10)) &&
        c.FormId % 100000 != 13110;

    public void Dispose() => _connection.Dispose();

    public sealed class CorpusCard
    {
        public long FormId { get; set; }
        public int Rank { get; set; }
        public int Gender { get; set; }
        public string Pattern { get; set; } = "";
    }
}
