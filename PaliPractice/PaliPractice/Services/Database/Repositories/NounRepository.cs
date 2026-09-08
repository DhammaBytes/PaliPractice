using PaliPractice.Models.Inflection;
using PaliPractice.Models.Words;
using PaliPractice.Services.Database.Entities;
using SQLite;

namespace PaliPractice.Services.Database.Repositories;

/// <summary>
/// Repository for noun data access and caching.
/// Caches are loaded lazily on first access.
/// </summary>
public class NounRepository : INounRepository
{
    /// <summary>
    /// Maximum number of ending variants for noun forms.
    /// Used for iterating over possible endings when checking attestation or retrieving forms.
    /// </summary>
    const int MaxNounEndings = 6;

    readonly SQLiteConnection _connection;
    readonly MeaningLoader _meanings;
    readonly Func<string> _language;
    readonly Lock _cacheLock = new();
    bool _isCacheLoaded;

    // Caches - loaded on first access
    CorpusFormIndex? _corpusForms;
    Dictionary<int, ILemma>? _lemmas;
    List<ILemma>? _lemmasByRank;
    HeadwordFormIndex? _irregularForms;

    public NounRepository(SQLiteConnection connection, Func<string>? language = null)
    {
        _connection = connection;
        _meanings = new MeaningLoader(connection);
        _language = language ?? (() => "en");
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

                // Build lemma objects grouping all noun variants
                var nouns = _connection.Table<Noun>().ToList();
                System.Diagnostics.Debug.WriteLine($"[NounRepo] Loaded {nouns.Count} noun records");

                _lemmas = nouns
                    .GroupBy(n => n.LemmaId)
                    .ToDictionary(
                        g => g.Key,
                        g => (ILemma)new Lemma(g.First().Lemma, g.Cast<IWord>()));
                System.Diagnostics.Debug.WriteLine($"[NounRepo] Built {_lemmas.Count} lemmas");

                var primaryHeadwords = _lemmas.Values.Select(l => l.Primary.Id).ToHashSet();
                _corpusForms = new CorpusFormIndex(_connection.Table<NounCorpusForm>()
                    .Select(f => (f.HeadwordId, (long)f.FormId)), primaryHeadwords);
                _irregularForms = new HeadwordFormIndex(_connection.Table<NounIrregularForm>()
                    .Select(f => new StoredHeadwordForm(f.HeadwordId, f.FormId, f.Form)));

                // Pre-sort for rank-based queries (tie-breaker ensures determinism)
                _lemmasByRank = _lemmas.Values
                    .OrderByDescending(l => l.EbtCount)
                    .ThenBy(l => l.LemmaId)
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
        return _corpusForms!.ContainsPrimary(formId);
    }

    public bool IsFormInCorpus(int lemmaId, Case @case, Gender gender, Number number, int endingIndex, int headwordId)
    {
        EnsureCacheLoaded();
        return _corpusForms!.Contains(headwordId, Declension.ResolveId(lemmaId, @case, gender, number, endingIndex));
    }

    public List<string> GetIrregularForms(int lemmaId, Case @case, Gender gender, Number number, int headwordId)
    {
        EnsureCacheLoaded();
        return _irregularForms!.GetForms(headwordId, Declension.ResolveId(lemmaId, @case, gender, number, 0), MaxNounEndings);
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
            if (_corpusForms!.ContainsPrimary(baseFormId + endingId))
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
        var baseFormId = Declension.ResolveId(lemmaId, @case, gender, number, 0);
        return _irregularForms!.GetForms(_lemmas![lemmaId].Primary.Id, baseFormId, MaxNounEndings);
    }

    /// <summary>
    /// Preload caches to avoid lazy loading delay on first access.
    /// Safe to call multiple times - subsequent calls are no-ops.
    /// </summary>
    public void Preload() => EnsureCacheLoaded();

    /// <summary>
    /// Ensure details are loaded for the lemma.
    /// Fetches from DB if not already loaded.
    /// </summary>
    public void EnsureDetails(ILemma lemma)
    {
        lock (_cacheLock)
        {
            if (!lemma.HasDetails)
            {
                var details = _connection.Query<NounDetails>(
                    "SELECT id, lemma_id, word, root, source_1, sutta_1, example_1, source_2, sutta_2, example_2 " +
                    "FROM nouns_details WHERE lemma_id=?", lemma.LemmaId);
                lemma.LoadDetails(details);
            }
            _meanings.Ensure(lemma, "nouns_details", _language());
        }
    }

    public void ClearMeaningCache()
    {
        lock (_cacheLock)
        {
            if (_lemmas is null) return;
            foreach (var lemma in _lemmas.Values)
                lemma.ClearMeanings();
        }
    }
}
