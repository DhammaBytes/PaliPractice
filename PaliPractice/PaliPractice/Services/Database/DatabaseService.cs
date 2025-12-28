using SQLite;
using System.Reflection;
using PaliPractice.Models.Inflection;
using PaliPractice.Models.Words;

namespace PaliPractice.Services.Database;

public interface IDatabaseService
{
    void Initialize();

    /// <summary>
    /// Get noun lemma by lemma ID. O(1) from cache.
    /// </summary>
    ILemma? GetNounLemma(int lemmaId);

    /// <summary>
    /// Get verb lemma by lemma ID. O(1) from cache.
    /// </summary>
    ILemma? GetVerbLemma(int lemmaId);

    /// <summary>
    /// Ensure details are loaded for the lemma.
    /// Fetches from DB if not already loaded.
    /// </summary>
    void EnsureDetails(ILemma lemma);

    int GetNounCount();
    int GetVerbCount();

    /// <summary>
    /// Get noun lemmas within a rank range, ordered by EbtCount descending.
    /// Rank 1 = most common noun.
    /// </summary>
    List<ILemma> GetNounLemmasByRank(int minRank, int maxRank);

    /// <summary>
    /// Get verb lemmas within a rank range, ordered by EbtCount descending.
    /// Rank 1 = most common verb.
    /// </summary>
    List<ILemma> GetVerbLemmasByRank(int minRank, int maxRank);

    /// <summary>
    /// Check if a verb lemma has reflexive conjugation forms.
    /// O(1) from cache.
    /// </summary>
    bool VerbHasReflexive(int lemmaId);

    /// <summary>
    /// Check if a specific noun form appears in the corpus.
    /// O(1) from cache.
    /// </summary>
    bool IsNounFormInCorpus(int lemmaId, Case @case, Gender gender, Number number, int endingIndex);

    /// <summary>
    /// Check if a specific verb form appears in the corpus.
    /// O(1) from cache.
    /// </summary>
    bool IsVerbFormInCorpus(int lemmaId, Tense tense, Person person, Number number, bool reflexive, int endingIndex);

    /// <summary>
    /// Check if any ending variant is attested for this noun form.
    /// O(1) from cache (checks up to 9 ending variants).
    /// </summary>
    bool HasAttestedNounForm(int lemmaId, Case @case, Gender gender, Number number);

    /// <summary>
    /// Check if any ending variant is attested for this verb form.
    /// O(1) from cache (checks up to 9 ending variants).
    /// </summary>
    bool HasAttestedVerbForm(int lemmaId, Tense tense, Person person, Number number, bool reflexive);

    /// <summary>
    /// Get all irregular noun forms for a specific grammatical combination.
    /// Returns empty list if not an irregular pattern or no forms found.
    /// </summary>
    List<string> GetIrregularNounForms(int lemmaId, Case @case, Gender gender, Number number);

    /// <summary>
    /// Get all irregular verb forms for a specific grammatical combination.
    /// Returns empty list if not an irregular pattern or no forms found.
    /// </summary>
    List<string> GetIrregularVerbForms(int lemmaId, Tense tense, Person person, Number number, bool reflexive);

    /// <summary>
    /// Check if irregular forms exist for this noun grammatical combination.
    /// Used to determine if a form exists for plural-only nouns, etc.
    /// </summary>
    bool HasIrregularNounForm(int lemmaId, Case @case, Gender gender, Number number);

    /// <summary>
    /// Check if irregular forms exist for this verb grammatical combination.
    /// Used to determine if a form exists for defective verbs.
    /// </summary>
    bool HasIrregularVerbForm(int lemmaId, Tense tense, Person person, Number number, bool reflexive);
}

public class DatabaseService : IDatabaseService
{
    SQLiteConnection? _database;
    readonly Lock _dbLock = new();
    readonly Lock _nounCacheLock = new();
    readonly Lock _verbCacheLock = new();
    bool _isDbInitialized;
    bool _isNounCacheLoaded;
    bool _isVerbCacheLoaded;

    // Noun caches - loaded lazily on first declension practice
    HashSet<int>? _attestedDeclensionFormIds;
    Dictionary<int, ILemma>? _nounLemmas;
    List<ILemma>? _nounLemmasByRank;
    Dictionary<int, string>? _irregularNounForms;  // form_id -> full form

    // Verb caches - loaded lazily on first conjugation practice
    HashSet<int>? _nonReflexiveLemmaIds;
    HashSet<long>? _attestedConjugationFormIds;
    Dictionary<int, ILemma>? _verbLemmas;
    List<ILemma>? _verbLemmasByRank;
    Dictionary<long, string>? _irregularVerbForms;  // form_id -> full form

    public void Initialize()
    {
        if (_isDbInitialized) return;

        lock (_dbLock)
        {
            if (_isDbInitialized) return;

            var databasePath = ExtractDatabase();
            _database = new SQLiteConnection(databasePath, SQLiteOpenFlags.ReadOnly);
            _isDbInitialized = true;
        }
    }

    /// <summary>
    /// Ensures noun caches are loaded. Called on first declension practice.
    /// </summary>
    void EnsureNounCacheLoaded()
    {
        if (_isNounCacheLoaded) return;

        lock (_nounCacheLock)
        {
            if (_isNounCacheLoaded) return;
            EnsureInitialized();

            // Pre-cache corpus attestation form_ids for O(1) lookup
            _attestedDeclensionFormIds = _database!
                .Table<NounCorpusForm>()
                .Select(d => d.FormId)
                .ToHashSet();

            // Pre-cache irregular noun forms for O(1) lookup
            _irregularNounForms = _database
                .Table<NounIrregularForm>()
                .ToDictionary(f => f.FormId, f => f.Form);

            // Build lemma objects grouping all noun variants
            _nounLemmas = _database
                .Table<Noun>()
                .ToList()
                .GroupBy(n => n.LemmaId)
                .ToDictionary(
                    g => g.Key,
                    g => (ILemma)new Lemma(g.First().Lemma, g.Cast<IWord>()));

            // Pre-sort for rank-based queries
            _nounLemmasByRank = _nounLemmas.Values
                .OrderByDescending(l => l.EbtCount)
                .ToList();

            _isNounCacheLoaded = true;
        }
    }

    /// <summary>
    /// Ensures verb caches are loaded. Called on first conjugation practice.
    /// </summary>
    void EnsureVerbCacheLoaded()
    {
        if (_isVerbCacheLoaded) return;

        lock (_verbCacheLock)
        {
            if (_isVerbCacheLoaded) return;
            EnsureInitialized();

            // Load non-reflexive verb lemma IDs (~28 entries)
            _nonReflexiveLemmaIds = _database!
                .Table<NonReflexiveVerb>()
                .Select(v => v.LemmaId)
                .ToHashSet();

            // Pre-cache corpus attestation form_ids for O(1) lookup
            _attestedConjugationFormIds = _database
                .Table<VerbCorpusForm>()
                .Select(c => c.FormId)
                .ToHashSet();

            // Pre-cache irregular verb forms for O(1) lookup
            _irregularVerbForms = _database
                .Table<VerbIrregularForm>()
                .ToDictionary(f => f.FormId, f => f.Form);

            // Build lemma objects grouping all verb variants
            _verbLemmas = _database
                .Table<Verb>()
                .ToList()
                .GroupBy(v => v.LemmaId)
                .ToDictionary(
                    g => g.Key,
                    g => (ILemma)new Lemma(g.First().Lemma, g.Cast<IWord>()));

            // Pre-sort for rank-based queries
            _verbLemmasByRank = _verbLemmas.Values
                .OrderByDescending(l => l.EbtCount)
                .ToList();

            _isVerbCacheLoaded = true;
        }
    }

    // Helper classes for LINQ queries on helper tables
    [Table("verbs_nonreflexive")]
    class NonReflexiveVerb
    {
        [Column("lemma_id")]
        public int LemmaId { get; set; }
    }

    string ExtractDatabase()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "PaliPractice.Data.training.db";

        // Get app data folder path and ensure PaliPractice subdirectory exists
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = System.IO.Path.Combine(appDataPath, "PaliPractice");

        // Create directory if it doesn't exist
        if (!Directory.Exists(appFolder))
        {
            Directory.CreateDirectory(appFolder);
        }

        var databasePath = System.IO.Path.Combine(appFolder, "training.db");

        // Only extract if file doesn't exist or is outdated
        if (!File.Exists(databasePath) || ShouldUpdateDatabase(databasePath))
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new FileNotFoundException($"Embedded resource '{resourceName}' not found");

            using var fileStream = File.Create(databasePath);
            stream.CopyTo(fileStream);
        }

        return databasePath;
    }

    bool ShouldUpdateDatabase(string databasePath)
    {
        // Always update in debug mode to ensure we're using the latest database
        #if DEBUG
        return true;
        #else
        // In production, check if the embedded resource is newer
        var fileInfo = new FileInfo(databasePath);
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyDate = File.GetLastWriteTime(assembly.Location);

        // If assembly is newer than database file, update it
        return assemblyDate > fileInfo.LastWriteTime;
        #endif
    }

    void EnsureInitialized()
    {
        if (!_isDbInitialized || _database == null)
            Initialize();
    }

    public ILemma? GetNounLemma(int lemmaId)
    {
        EnsureNounCacheLoaded();
        return _nounLemmas!.GetValueOrDefault(lemmaId);
    }

    public ILemma? GetVerbLemma(int lemmaId)
    {
        EnsureVerbCacheLoaded();
        return _verbLemmas!.GetValueOrDefault(lemmaId);
    }

    public void EnsureDetails(ILemma lemma)
    {
        if (lemma.HasDetails) return;

        EnsureInitialized();

        // Determine if this is a noun or verb lemma by checking the first word
        var firstWord = lemma.Primary;
        IReadOnlyList<IWordDetails> details = firstWord switch
        {
            Noun => _database!.Table<NounDetails>()
                .Where(d => d.LemmaId == lemma.LemmaId)
                .ToList(),
            Verb => _database!.Table<VerbDetails>()
                .Where(d => d.LemmaId == lemma.LemmaId)
                .ToList(),
            _ => []
        };

        lemma.LoadDetails(details);
    }

    public int GetNounCount()
    {
        EnsureNounCacheLoaded();
        return _nounLemmas!.Count;
    }

    public int GetVerbCount()
    {
        EnsureVerbCacheLoaded();
        return _verbLemmas!.Count;
    }

    public List<ILemma> GetNounLemmasByRank(int minRank, int maxRank)
    {
        EnsureNounCacheLoaded();
        return _nounLemmasByRank!
            .Skip(minRank - 1)
            .Take(maxRank - minRank + 1)
            .ToList();
    }

    public List<ILemma> GetVerbLemmasByRank(int minRank, int maxRank)
    {
        EnsureVerbCacheLoaded();
        return _verbLemmasByRank!
            .Skip(minRank - 1)
            .Take(maxRank - minRank + 1)
            .ToList();
    }

    public bool VerbHasReflexive(int lemmaId)
    {
        EnsureVerbCacheLoaded();
        return !_nonReflexiveLemmaIds!.Contains(lemmaId);
    }

    public bool IsNounFormInCorpus(
        int lemmaId,
        Case @case,
        Gender gender,
        Number number,
        int endingIndex)
    {
        EnsureNounCacheLoaded();
        var formId = Declension.ResolveId(lemmaId, @case, gender, number, endingIndex);
        return _attestedDeclensionFormIds!.Contains(formId);
    }

    public bool IsVerbFormInCorpus(
        int lemmaId,
        Tense tense,
        Person person,
        Number number,
        bool reflexive,
        int endingIndex)
    {
        EnsureVerbCacheLoaded();
        var formId = Conjugation.ResolveId(lemmaId, tense, person, number, reflexive, endingIndex);
        return _attestedConjugationFormIds!.Contains(formId);
    }

    public bool HasAttestedNounForm(int lemmaId, Case @case, Gender gender, Number number)
    {
        EnsureNounCacheLoaded();
        // Check if ANY ending variant (1-9) is attested - O(9) HashSet lookups
        var baseFormId = Declension.ResolveId(lemmaId, @case, gender, number, 0);
        for (int endingId = 1; endingId <= 9; endingId++)
        {
            if (_attestedDeclensionFormIds!.Contains(baseFormId + endingId))
                return true;
        }
        return false;
    }

    public bool HasAttestedVerbForm(int lemmaId, Tense tense, Person person, Number number, bool reflexive)
    {
        EnsureVerbCacheLoaded();
        // Check if ANY ending variant (1-9) is attested - O(9) HashSet lookups
        var baseFormId = Conjugation.ResolveId(lemmaId, tense, person, number, reflexive, 0);
        for (int endingId = 1; endingId <= 9; endingId++)
        {
            if (_attestedConjugationFormIds!.Contains(baseFormId + endingId))
                return true;
        }
        return false;
    }

    public List<string> GetIrregularNounForms(int lemmaId, Case @case, Gender gender, Number number)
    {
        EnsureNounCacheLoaded();
        var forms = new List<string>();
        var baseFormId = Declension.ResolveId(lemmaId, @case, gender, number, 0);
        for (int endingId = 1; endingId <= 9; endingId++)
        {
            if (_irregularNounForms!.TryGetValue(baseFormId + endingId, out var form))
                forms.Add(form);
        }
        return forms;
    }

    public List<string> GetIrregularVerbForms(int lemmaId, Tense tense, Person person, Number number, bool reflexive)
    {
        EnsureVerbCacheLoaded();
        var forms = new List<string>();
        var baseFormId = Conjugation.ResolveId(lemmaId, tense, person, number, reflexive, 0);
        for (int endingId = 1; endingId <= 9; endingId++)
        {
            if (_irregularVerbForms!.TryGetValue(baseFormId + endingId, out var form))
                forms.Add(form);
        }
        return forms;
    }

    public bool HasIrregularNounForm(int lemmaId, Case @case, Gender gender, Number number)
    {
        EnsureNounCacheLoaded();
        var baseFormId = Declension.ResolveId(lemmaId, @case, gender, number, 0);
        for (int endingId = 1; endingId <= 9; endingId++)
        {
            if (_irregularNounForms!.ContainsKey(baseFormId + endingId))
                return true;
        }
        return false;
    }

    public bool HasIrregularVerbForm(int lemmaId, Tense tense, Person person, Number number, bool reflexive)
    {
        EnsureVerbCacheLoaded();
        var baseFormId = Conjugation.ResolveId(lemmaId, tense, person, number, reflexive, 0);
        for (int endingId = 1; endingId <= 9; endingId++)
        {
            if (_irregularVerbForms!.ContainsKey(baseFormId + endingId))
                return true;
        }
        return false;
    }

    public void Dispose()
    {
        _database?.Close();
    }
}
