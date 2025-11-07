using SQLite;
using System.Reflection;

namespace PaliPractice.Services;

public interface IDatabaseService
{
    Task InitializeAsync();
    Task<List<Noun>> GetRandomNounsAsync(int count = 10);
    Task<List<Verb>> GetRandomVerbsAsync(int count = 10);
    Task<Noun?> GetNounByIdAsync(int id);
    Task<Verb?> GetVerbByIdAsync(int id);
    Task<List<Declension>> GetDeclensionsByNounIdAsync(int nounId);
    Task<List<Conjugation>> GetConjugationsByVerbIdAsync(int verbId);
    Task<int> GetNounCountAsync();
    Task<int> GetVerbCountAsync();
}

public class DatabaseService : IDatabaseService
{
    SQLiteAsyncConnection? _database;
    readonly SemaphoreSlim _initSemaphore = new(1, 1);
    bool _isInitialized = false;
    
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;
        
        await _initSemaphore.WaitAsync();
        try 
        {
            if (_isInitialized) return;
            
            // Extract embedded database to app data folder
            var databasePath = await ExtractDatabaseAsync();
            
            // Create SQLite connection
            _database = new SQLiteAsyncConnection(databasePath, SQLiteOpenFlags.ReadOnly);
            
            _isInitialized = true;
        }
        finally
        {
            _initSemaphore.Release();
        }
    }

    async Task<string> ExtractDatabaseAsync()
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
            await stream.CopyToAsync(fileStream);
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

    async Task EnsureInitializedAsync()
    {
        if (!_isInitialized || _database == null)
            await InitializeAsync();
    }
    
    public async Task<List<Noun>> GetRandomNounsAsync(int count = 10)
    {
        await EnsureInitializedAsync();

        return await _database!.Table<Noun>()
            .OrderByDescending(n => n.EbtCount)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<Verb>> GetRandomVerbsAsync(int count = 10)
    {
        await EnsureInitializedAsync();

        return await _database!.Table<Verb>()
            .OrderByDescending(v => v.EbtCount)
            .Take(count)
            .ToListAsync();
    }

    public async Task<Noun?> GetNounByIdAsync(int id)
    {
        await EnsureInitializedAsync();

        return await _database!.Table<Noun>()
            .Where(n => n.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<Verb?> GetVerbByIdAsync(int id)
    {
        await EnsureInitializedAsync();

        return await _database!.Table<Verb>()
            .Where(v => v.Id == id)
            .FirstOrDefaultAsync();
    }
    
    public async Task<List<Declension>> GetDeclensionsByNounIdAsync(int nounId)
    {
        await EnsureInitializedAsync();

        return await _database!.Table<Declension>()
            .Where(d => d.NounId == nounId)
            .OrderBy(d => d.CaseName)
            .ThenBy(d => d.Number)
            .ToListAsync();
    }

    public async Task<List<Conjugation>> GetConjugationsByVerbIdAsync(int verbId)
    {
        await EnsureInitializedAsync();

        return await _database!.Table<Conjugation>()
            .Where(c => c.VerbId == verbId)
            .OrderBy(c => c.Person)
            .ThenBy(c => c.Tense)
            .ThenBy(c => c.Mood)
            .ToListAsync();
    }
    
    public async Task<int> GetNounCountAsync()
    {
        await EnsureInitializedAsync();

        return await _database!.Table<Noun>()
            .CountAsync();
    }

    public async Task<int> GetVerbCountAsync()
    {
        await EnsureInitializedAsync();

        return await _database!.Table<Verb>()
            .CountAsync();
    }
    
    public void Dispose()
    {
        _database?.CloseAsync();
        _initSemaphore.Dispose();
    }
}
