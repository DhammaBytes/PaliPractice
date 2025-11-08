using SQLite;
using System.Reflection;

namespace PaliPractice.Services.Database;

public interface IDatabaseService
{
    void Initialize();
    List<Noun> GetRandomNouns(int count = 10);
    List<Verb> GetRandomVerbs(int count = 10);
    Noun? GetNounById(int id);
    Verb? GetVerbById(int id);
    int GetNounCount();
    int GetVerbCount();

    /// <summary>
    /// Check if a specific noun form appears in the Pali Tipitaka corpus.
    /// </summary>
    bool IsNounFormInCorpus(int nounId, NounCase nounCase, Number number, Gender gender, int endingIndex = 0);

    /// <summary>
    /// Check if a specific verb form appears in the Pali Tipitaka corpus.
    /// </summary>
    bool IsVerbFormInCorpus(int verbId, Person person, Tense tense, Voice voice, int endingIndex = 0);
}

public class DatabaseService : IDatabaseService
{
    SQLiteConnection? _database;
    readonly object _initLock = new();
    bool _isInitialized = false;

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

            _isInitialized = true;
        }
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

        return _database!.Table<Noun>()
            .Where(n => n.Id == id)
            .FirstOrDefault();
    }

    public Verb? GetVerbById(int id)
    {
        EnsureInitialized();

        return _database!.Table<Verb>()
            .Where(v => v.Id == id)
            .FirstOrDefault();
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

    public bool IsNounFormInCorpus(
        int nounId,
        NounCase nounCase,
        Number number,
        Gender gender,
        int endingIndex = 0)
    {
        EnsureInitialized();

        var result = _database!.Table<CorpusDeclension>()
            .Where(d => d.NounId == nounId
                     && d.CaseName == (int)nounCase
                     && d.Number == (int)number
                     && d.Gender == (int)gender
                     && d.EndingIndex == endingIndex)
            .Count();

        return result > 0;
    }

    public bool IsVerbFormInCorpus(
        int verbId,
        Person person,
        Tense tense,
        Voice voice,
        int endingIndex = 0)
    {
        EnsureInitialized();

        var result = _database!.Table<CorpusConjugation>()
            .Where(c => c.VerbId == verbId
                     && c.Person == (int)person
                     && c.Tense == (int)tense
                     && c.Voice == (int)voice
                     && c.EndingIndex == endingIndex)
            .Count();

        return result > 0;
    }

    public void Dispose()
    {
        _database?.Close();
    }
}
