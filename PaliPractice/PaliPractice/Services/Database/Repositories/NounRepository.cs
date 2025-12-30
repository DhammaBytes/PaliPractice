using PaliPractice.Models.Inflection;
using PaliPractice.Models.Words;
using PaliPractice.Services.Database.Entities;
using SQLite;

namespace PaliPractice.Services.Database.Repositories;

/// <summary>
/// Repository for noun data access and caching.
/// Caches are loaded lazily on first access.
/// </summary>
public class NounRepository
{
    /// <summary>
    /// Maximum number of ending variants for noun forms.
    /// Used for iterating over possible endings when checking attestation or retrieving forms.
    /// </summary>
    const int MaxNounEndings = 6;

    readonly SQLiteConnection _connection;
    readonly Lock _cacheLock = new();
    bool _isCacheLoaded;

    // Caches - loaded on first access
    HashSet<int>? _corpusFormIds;
    Dictionary<int, ILemma>? _lemmas;
    List<ILemma>? _lemmasByRank;
    Dictionary<int, string>? _irregularForms;

    public NounRepository(SQLiteConnection connection)
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
                System.Diagnostics.Debug.WriteLine("[NounRepo] Loading caches...");

                // Pre-cache corpus attestation form_ids for O(1) lookup
                _corpusFormIds = _connection
                    .Table<NounCorpusForm>()
                    .Select(d => d.FormId)
                    .ToHashSet();
                System.Diagnostics.Debug.WriteLine($"[NounRepo] Loaded {_corpusFormIds.Count} corpus form IDs");

                // Pre-cache irregular noun forms for O(1) lookup
                _irregularForms = _connection
                    .Table<NounIrregularForm>()
                    .ToDictionary(f => f.FormId, f => f.Form);
                System.Diagnostics.Debug.WriteLine($"[NounRepo] Loaded {_irregularForms.Count} irregular forms");

                // Build lemma objects grouping all noun variants
                var nouns = _connection.Table<Noun>().ToList();
                System.Diagnostics.Debug.WriteLine($"[NounRepo] Loaded {nouns.Count} noun records");

                _lemmas = nouns
                    .GroupBy(n => n.LemmaId)
                    .ToDictionary(
                        g => g.Key,
                        g => (ILemma)new Lemma(g.First().Lemma, g.Cast<IWord>()));
                System.Diagnostics.Debug.WriteLine($"[NounRepo] Built {_lemmas.Count} lemmas");

                // Pre-sort for rank-based queries
                _lemmasByRank = _lemmas.Values
                    .OrderByDescending(l => l.EbtCount)
                    .ToList();

                _isCacheLoaded = true;
                System.Diagnostics.Debug.WriteLine("[NounRepo] Cache loaded successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NounRepo] Cache load FAILED: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[NounRepo] Stack: {ex.StackTrace}");
                throw;
            }
        }
    }

    /// <summary>
    /// Get noun lemma by lemma ID. O(1) from cache.
    /// </summary>
    public ILemma? GetLemma(int lemmaId)
    {
        EnsureCacheLoaded();
        return _lemmas!.GetValueOrDefault(lemmaId);
    }

    /// <summary>
    /// Get total count of noun lemmas.
    /// </summary>
    public int GetCount()
    {
        EnsureCacheLoaded();
        return _lemmas!.Count;
    }

    /// <summary>
    /// Get noun lemmas within a rank range, ordered by EbtCount descending.
    /// Rank 1 = most common noun.
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
    /// Check if a specific noun form appears in the corpus.
    /// O(1) from cache.
    /// </summary>
    public bool IsFormInCorpus(int lemmaId, Case @case, Gender gender, Number number, int endingIndex)
    {
        EnsureCacheLoaded();
        var formId = Declension.ResolveId(lemmaId, @case, gender, number, endingIndex);
        return _corpusFormIds!.Contains(formId);
    }

    /// <summary>
    /// Check if any ending variant is attested for this noun form.
    /// O(1) from cache.
    /// </summary>
    public bool HasAttestedForm(int lemmaId, Case @case, Gender gender, Number number)
    {
        EnsureCacheLoaded();
        var baseFormId = Declension.ResolveId(lemmaId, @case, gender, number, 0);
        for (int endingId = 1; endingId <= MaxNounEndings; endingId++)
        {
            if (_corpusFormIds!.Contains(baseFormId + endingId))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Get all irregular noun forms for a specific grammatical combination.
    /// Returns empty list if not an irregular pattern or no forms found.
    /// </summary>
    public List<string> GetIrregularForms(int lemmaId, Case @case, Gender gender, Number number)
    {
        EnsureCacheLoaded();
        var forms = new List<string>();
        var baseFormId = Declension.ResolveId(lemmaId, @case, gender, number, 0);
        for (int endingId = 1; endingId <= MaxNounEndings; endingId++)
        {
            if (_irregularForms!.TryGetValue(baseFormId + endingId, out var form))
                forms.Add(form);
        }
        return forms;
    }

    /// <summary>
    /// Check if irregular forms exist for this noun grammatical combination.
    /// Used to determine if a form exists for plural-only nouns, etc.
    /// </summary>
    public bool HasIrregularForm(int lemmaId, Case @case, Gender gender, Number number)
    {
        EnsureCacheLoaded();
        var baseFormId = Declension.ResolveId(lemmaId, @case, gender, number, 0);
        for (int endingId = 1; endingId <= MaxNounEndings; endingId++)
        {
            if (_irregularForms!.ContainsKey(baseFormId + endingId))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Ensure details are loaded for the lemma.
    /// Fetches from DB if not already loaded.
    /// </summary>
    public void EnsureDetails(ILemma lemma)
    {
        if (lemma.HasDetails) return;

        IReadOnlyList<IWordDetails> details = _connection
            .Table<NounDetails>()
            .Where(d => d.LemmaId == lemma.LemmaId)
            .ToList();

        lemma.LoadDetails(details);
    }
}
