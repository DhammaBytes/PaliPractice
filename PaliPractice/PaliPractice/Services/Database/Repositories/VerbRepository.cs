using PaliPractice.Models.Inflection;
using PaliPractice.Models.Words;
using PaliPractice.Services.Database.Entities;
using SQLite;

namespace PaliPractice.Services.Database.Repositories;

/// <summary>
/// Helper class for LINQ queries on verbs_nonreflexive table.
/// </summary>
[Table("verbs_nonreflexive")]
class NonReflexiveVerb
{
    [Column("lemma_id")]
    public int LemmaId { get; set; }
}

/// <summary>
/// Repository for verb data access and caching.
/// Caches are loaded lazily on first access.
/// </summary>
public class VerbRepository
{
    readonly SQLiteConnection _connection;
    readonly Lock _cacheLock = new();
    bool _isCacheLoaded;

    // Caches - loaded on first access
    HashSet<int>? _nonReflexiveLemmaIds;
    HashSet<long>? _corpusFormIds;
    Dictionary<int, ILemma>? _lemmas;
    List<ILemma>? _lemmasByRank;
    Dictionary<long, string>? _irregularForms;

    public VerbRepository(SQLiteConnection connection)
    {
        _connection = connection;
    }

    /// <summary>
    /// Ensures all caches are loaded. Called automatically on first access.
    /// </summary>
    void EnsureCacheLoaded()
    {
        if (_isCacheLoaded) return;

        lock (_cacheLock)
        {
            if (_isCacheLoaded) return;

            try
            {
                System.Diagnostics.Debug.WriteLine("[VerbRepo] Loading caches...");

                // Load non-reflexive verb lemma IDs (~28 entries)
                _nonReflexiveLemmaIds = _connection
                    .Table<NonReflexiveVerb>()
                    .Select(v => v.LemmaId)
                    .ToHashSet();
                System.Diagnostics.Debug.WriteLine($"[VerbRepo] Loaded {_nonReflexiveLemmaIds.Count} non-reflexive lemma IDs");

                // Pre-cache corpus attestation form_ids for O(1) lookup
                _corpusFormIds = _connection
                    .Table<VerbCorpusForm>()
                    .Select(c => c.FormId)
                    .ToHashSet();
                System.Diagnostics.Debug.WriteLine($"[VerbRepo] Loaded {_corpusFormIds.Count} corpus form IDs");

                // Pre-cache irregular verb forms for O(1) lookup
                _irregularForms = _connection
                    .Table<VerbIrregularForm>()
                    .ToDictionary(f => f.FormId, f => f.Form);
                System.Diagnostics.Debug.WriteLine($"[VerbRepo] Loaded {_irregularForms.Count} irregular forms");

                // Build lemma objects grouping all verb variants
                var verbs = _connection.Table<Verb>().ToList();
                System.Diagnostics.Debug.WriteLine($"[VerbRepo] Loaded {verbs.Count} verb records");

                _lemmas = verbs
                    .GroupBy(v => v.LemmaId)
                    .ToDictionary(
                        g => g.Key, ILemma (g) => new Lemma(g.First().Lemma, g));
                System.Diagnostics.Debug.WriteLine($"[VerbRepo] Built {_lemmas.Count} lemmas");

                // Pre-sort for rank-based queries
                _lemmasByRank = _lemmas.Values
                    .OrderByDescending(l => l.EbtCount)
                    .ToList();

                _isCacheLoaded = true;
                System.Diagnostics.Debug.WriteLine("[VerbRepo] Cache loaded successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VerbRepo] Cache load FAILED: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[VerbRepo] Stack: {ex.StackTrace}");
                throw;
            }
        }
    }

    /// <summary>
    /// Get verb lemma by lemma ID. O(1) from cache.
    /// </summary>
    public ILemma? GetLemma(int lemmaId)
    {
        EnsureCacheLoaded();
        return _lemmas!.GetValueOrDefault(lemmaId);
    }

    /// <summary>
    /// Get total count of verb lemmas.
    /// </summary>
    public int GetCount()
    {
        EnsureCacheLoaded();
        return _lemmas!.Count;
    }

    /// <summary>
    /// Get verb lemmas within a rank range, ordered by EbtCount descending.
    /// Rank 1 = most common verb.
    /// </summary>
    public List<ILemma> GetLemmasByRank(int minRank, int maxRank)
    {
        EnsureCacheLoaded();
        return _lemmasByRank!
            .Skip(minRank - 1)
            .Take(maxRank - minRank + 1)
            .ToList();
    }

    /// <summary>
    /// Check if a verb lemma has reflexive conjugation forms.
    /// O(1) from cache.
    /// </summary>
    public bool HasReflexive(int lemmaId)
    {
        EnsureCacheLoaded();
        return !_nonReflexiveLemmaIds!.Contains(lemmaId);
    }

    /// <summary>
    /// Check if a specific verb form appears in the corpus.
    /// O(1) from the cache.
    /// </summary>
    public bool IsFormInCorpus(int lemmaId, Tense tense, Person person, Number number, bool reflexive, int endingIndex)
    {
        EnsureCacheLoaded();
        var voice = reflexive ? Voice.Reflexive : Voice.Active;
        var formId = Conjugation.ResolveId(lemmaId, tense, person, number, voice, endingIndex);
        return _corpusFormIds!.Contains(formId);
    }

    /// <summary>
    /// Check if any ending variant is in corpus for this verb form.
    /// O(1) from cache (checks up to 7 ending variants because of "atthi", though only 5 of them are in the corpus).
    /// </summary>
    public bool HasAttestedForm(int lemmaId, Tense tense, Person person, Number number, bool reflexive)
    {
        EnsureCacheLoaded();
        var voice = reflexive ? Voice.Reflexive : Voice.Active;
        var baseFormId = Conjugation.ResolveId(lemmaId, tense, person, number, voice, 0);
        for (int endingId = 1; endingId <= 7; endingId++)
        {
            if (_corpusFormIds!.Contains(baseFormId + endingId))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Get all irregular verb forms for a specific grammatical combination.
    /// Returns an empty list if not an irregular pattern or no forms found.
    /// </summary>
    public List<string> GetIrregularForms(int lemmaId, Tense tense, Person person, Number number, bool reflexive)
    {
        EnsureCacheLoaded();
        var forms = new List<string>();
        var voice = reflexive ? Voice.Reflexive : Voice.Active;
        var baseFormId = Conjugation.ResolveId(lemmaId, tense, person, number, voice, 0);
        for (int endingId = 1; endingId <= 9; endingId++)
        {
            if (_irregularForms!.TryGetValue(baseFormId + endingId, out var form))
                forms.Add(form);
        }
        return forms;
    }

    // /// <summary>
    // /// Check if irregular forms exist for this verb grammatical combination.
    // /// Used to determine if a form exists for defective verbs.
    // /// </summary>
    // public bool HasIrregularForm(int lemmaId, Tense tense, Person person, Number number, bool reflexive)
    // {
    //     EnsureCacheLoaded();
    //     var voice = reflexive ? Voice.Reflexive : Voice.Active;
    //     var baseFormId = Conjugation.ResolveId(lemmaId, tense, person, number, voice, 0);
    //     for (int endingId = 1; endingId <= 9; endingId++)
    //     {
    //         if (_irregularForms!.ContainsKey(baseFormId + endingId))
    //             return true;
    //     }
    //     return false;
    // }

    /// <summary>
    /// Ensure details are loaded for the lemma.
    /// Fetches from DB if not already loaded.
    /// </summary>
    public void EnsureDetails(ILemma lemma)
    {
        if (lemma.HasDetails) return;

        IReadOnlyList<IWordDetails> details = _connection
            .Table<VerbDetails>()
            .Where(d => d.LemmaId == lemma.LemmaId)
            .ToList();

        lemma.LoadDetails(details);
    }
}
