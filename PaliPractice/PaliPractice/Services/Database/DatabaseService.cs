using SQLite;
using System.Reflection;
using PaliPractice.Models.Inflection;
using PaliPractice.Models.Words;

namespace PaliPractice.Services.Database;

public interface IDatabaseService
{
    void Initialize();
    List<Noun> GetRandomNouns(int count = 10);
    List<Verb> GetRandomVerbs(int count = 10);
    Noun? GetNounById(int id);
    Verb? GetVerbById(int id);
    Noun? GetNounByLemmaId(int lemmaId);
    Verb? GetVerbByLemmaId(int lemmaId);
    int GetNounCount();
    int GetVerbCount();

    /// <summary>
    /// Get nouns within a rank range, ordered by EbtCount descending.
    /// Rank 1 = most common noun.
    /// </summary>
    List<Noun> GetNounsByRank(int minRank, int maxRank);

    /// <summary>
    /// Get verbs within a rank range, ordered by EbtCount descending.
    /// Rank 1 = most common verb.
    /// </summary>
    List<Verb> GetVerbsByRank(int minRank, int maxRank);

    /// <summary>
    /// Check if a verb lemma has reflexive conjugation forms.
    /// Most verbs do have reflexive forms; only ~28 lemmas are active-only.
    /// </summary>
    bool VerbHasReflexive(int lemmaId);

    /// <summary>
    /// Check if a specific noun form appears in the Pali Tipitaka corpus.
    /// Uses form_id lookup for efficient querying.
    /// </summary>
    bool IsNounFormInCorpus(int lemmaId, Case @case, Gender gender, Number number, int endingIndex);

    /// <summary>
    /// Check if a specific verb form appears in the Pali Tipitaka corpus.
    /// Uses form_id lookup for efficient querying.
    /// </summary>
    bool IsVerbFormInCorpus(int lemmaId, Tense tense, Person person, Number number, bool reflexive, int endingIndex);

    /// <summary>
    /// Check if a noun form (without ending) is attested in the corpus.
    /// </summary>
    bool HasAttestedNounForm(int lemmaId, Case @case, Gender gender, Number number);

    /// <summary>
    /// Check if a verb form (without ending) is attested in the corpus.
    /// </summary>
    bool HasAttestedVerbForm(int lemmaId, Tense tense, Person person, Number number, bool reflexive);
}

public class DatabaseService : IDatabaseService
{
    SQLiteConnection? _database;
    readonly Lock _initLock = new();
    bool _isInitialized = false;

    // Cache of verb lemma_ids that do NOT have reflexive forms (the minority)
    HashSet<int>? _nonReflexiveLemmaIds;

    public void Initialize()
    {
        if (_isInitialized) return;

        lock (_initLock)
        {
            if (_isInitialized) return;

            // Extract embedded database to app data folder
            var databasePath = ExtractDatabase();

            // Create SQLite connection
            _database = new SQLiteConnection(databasePath, SQLiteOpenFlags.ReadOnly);

            // Load non-reflexive verb lemma IDs into HashSet for O(1) lookup
            _nonReflexiveLemmaIds = _database
                .Query<NonReflexiveVerb>("SELECT lemma_id FROM verbs_nonreflexive")
                .Select(v => v.LemmaId)
                .ToHashSet();

            _isInitialized = true;
        }
    }

    // Helper class for querying verbs_nonreflexive table
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
        if (!_isInitialized || _database == null)
            Initialize();
    }

    public List<Noun> GetRandomNouns(int count = 10)
    {
        EnsureInitialized();

        return _database!.Table<Noun>()
            .OrderByDescending(n => n.EbtCount)
            .Take(count)
            .ToList();
    }

    public List<Verb> GetRandomVerbs(int count = 10)
    {
        EnsureInitialized();

        return _database!.Table<Verb>()
            .OrderByDescending(v => v.EbtCount)
            .Take(count)
            .ToList();
    }

    public Noun? GetNounById(int id)
    {
        EnsureInitialized();

        return _database!
            .Table<Noun>()
            .FirstOrDefault(n => n.Id == id);
    }

    public Verb? GetVerbById(int id)
    {
        EnsureInitialized();

        return _database!
            .Table<Verb>()
            .FirstOrDefault(v => v.Id == id);
    }

    public Noun? GetNounByLemmaId(int lemmaId)
    {
        EnsureInitialized();

        return _database!
            .Table<Noun>()
            .FirstOrDefault(n => n.LemmaId == lemmaId);
    }

    public Verb? GetVerbByLemmaId(int lemmaId)
    {
        EnsureInitialized();

        return _database!
            .Table<Verb>()
            .FirstOrDefault(v => v.LemmaId == lemmaId);
    }

    public int GetNounCount()
    {
        EnsureInitialized();

        return _database!.Table<Noun>()
            .Count();
    }

    public int GetVerbCount()
    {
        EnsureInitialized();

        return _database!.Table<Verb>()
            .Count();
    }

    public List<Noun> GetNounsByRank(int minRank, int maxRank)
    {
        EnsureInitialized();

        // Rank is 1-indexed: rank 1 = highest EbtCount
        // Skip (minRank - 1) items, take (maxRank - minRank + 1) items
        return _database!.Table<Noun>()
            .OrderByDescending(n => n.EbtCount)
            .Skip(minRank - 1)
            .Take(maxRank - minRank + 1)
            .ToList();
    }

    public List<Verb> GetVerbsByRank(int minRank, int maxRank)
    {
        EnsureInitialized();

        // Rank is 1-indexed: rank 1 = highest EbtCount
        // Skip (minRank - 1) items, take (maxRank - minRank + 1) items
        return _database!.Table<Verb>()
            .OrderByDescending(v => v.EbtCount)
            .Skip(minRank - 1)
            .Take(maxRank - minRank + 1)
            .ToList();
    }

    public bool VerbHasReflexive(int lemmaId)
    {
        EnsureInitialized();

        // Most verbs have reflexive forms, so we check if NOT in the exceptions set
        return !_nonReflexiveLemmaIds!.Contains(lemmaId);
    }

    public bool IsNounFormInCorpus(
        int lemmaId,
        Case @case,
        Gender gender,
        Number number,
        int endingIndex)
    {
        EnsureInitialized();

        var formId = Declension.ResolveId(lemmaId, @case, gender, number, endingIndex);
        return _database!
            .Table<CorpusDeclension>()
            .Any(d => d.FormId == formId);
    }

    public bool IsVerbFormInCorpus(
        int lemmaId,
        Tense tense,
        Person person,
        Number number,
        bool reflexive,
        int endingIndex)
    {
        EnsureInitialized();

        var formId = Conjugation.ResolveId(lemmaId, tense, person, number, reflexive, endingIndex);
        return _database!
            .Table<CorpusConjugation>()
            .Any(c => c.FormId == formId);
    }

    public bool HasAttestedNounForm(int lemmaId, Case @case, Gender gender, Number number)
    {
        EnsureInitialized();

        // Check if ANY ending variant (1-9) is attested for this combination
        // We query for the base formId range (endingId 1-9)
        var baseFormId = Declension.ResolveId(lemmaId, @case, gender, number, 0);
        var minFormId = baseFormId + 1;  // EndingId 1
        var maxFormId = baseFormId + 9;  // EndingId 9 (max possible)

        return _database!
            .Table<CorpusDeclension>()
            .Any(d => d.FormId >= minFormId && d.FormId <= maxFormId);
    }

    public bool HasAttestedVerbForm(int lemmaId, Tense tense, Person person, Number number, bool reflexive)
    {
        EnsureInitialized();

        // Check if ANY ending variant (1-9) is attested for this combination
        var baseFormId = Conjugation.ResolveId(lemmaId, tense, person, number, reflexive, 0);
        var minFormId = baseFormId + 1;  // EndingId 1
        var maxFormId = baseFormId + 9;  // EndingId 9 (max possible)

        return _database!
            .Table<CorpusConjugation>()
            .Any(c => c.FormId >= minFormId && c.FormId <= maxFormId);
    }

    public void Dispose()
    {
        _database?.Close();
    }
}
