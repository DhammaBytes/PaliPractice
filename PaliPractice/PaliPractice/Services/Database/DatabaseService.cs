using PaliPractice.Services.Database.Providers;
using PaliPractice.Services.Database.Repositories;
using PaliPractice.Services.UserData.Entities;
using SQLite;
using IOPath = System.IO.Path;
using IOFile = System.IO.File;

namespace PaliPractice.Services.Database;

/// <summary>
/// Unified database service that provisions and provides access to all databases.
/// Pali database uses platform-specific asset types in csproj:
/// - Desktop/WASM: Content with CopyToOutputDirectory
/// - iOS: BundleResource (read from bundle)
/// - Android: AndroidAsset (copied to local storage)
/// </summary>
public interface IDatabaseService
{
    /// <summary>
    /// Repository for noun data (lemmas, forms, attestations).
    /// </summary>
    NounRepository Nouns { get; }

    /// <summary>
    /// Repository for verb data (lemmas, forms, attestations).
    /// </summary>
    VerbRepository Verbs { get; }

    /// <summary>
    /// Repository for user data (mastery, settings, history).
    /// </summary>
    UserDataRepository UserData { get; }

    /// <summary>
    /// True if a critical failure occurred during provisioning.
    /// App should show an error screen if true.
    /// </summary>
    bool HasFatalFailure { get; }

    /// <summary>
    /// Log of all provisioning events for diagnostics.
    /// </summary>
    IReadOnlyList<DatabaseProvisionedEvent> ProvisionLog { get; }
}

/// <summary>
/// Database service that handles provisioning and exposes repositories.
/// </summary>
public class DatabaseService : IDatabaseService
{
    /// <summary>
    /// Bundle version number. Increment when pali.db schema or data changes.
    /// Used to determine if cached database needs replacement.
    /// </summary>
    public const int BundleVersion = 1;

    public NounRepository Nouns { get; }
    public VerbRepository Verbs { get; }
    public UserDataRepository UserData { get; }

    public bool HasFatalFailure => _provisionLog.Any(e => e.IsFailure);
    public IReadOnlyList<DatabaseProvisionedEvent> ProvisionLog => _provisionLog;

    readonly List<DatabaseProvisionedEvent> _provisionLog = [];
    readonly IBundledFileProvider _bundledFileProvider;

    public DatabaseService(IBundledFileProvider bundledFileProvider)
    {
        _bundledFileProvider = bundledFileProvider;

        // Provision grammar database (read-only)
        SQLiteConnection paliDb;
        try
        {
            paliDb = OpenBundledDatabase(DatabaseFile.Pali);
        }
        catch (Exception)
        {
            paliDb = CreateEmptyDatabase(DatabaseFile.Pali);
        }

        // Provision user data database (writable)
        // No fallback - if this fails, the app cannot function properly
        var userDataDb = OpenWritableDatabase(DatabaseFile.UserData);

        // Create repositories
        Nouns = new NounRepository(paliDb);
        Verbs = new VerbRepository(paliDb);
        UserData = new UserDataRepository(userDataDb);

        // Initialize default settings if first run
        UserData.InitializeDefaultsIfNeeded();

        // Log table counts for diagnostics
        LogTableCounts(paliDb, "pali.db");
    }

    SQLiteConnection OpenWritableDatabase(DatabaseFile file)
    {
        try
        {
            if (file.Behavior != DatabaseBehavior.CreatedUserWritable)
                throw new InvalidOperationException($"'{file.Name}' is not writable. Behavior: {file.Behavior}");

            var path = IOPath.Combine(_bundledFileProvider.GetUserDataDirectory(), file.Name);
            var existed = IOFile.Exists(path);
            var connection = new SQLiteConnection(path);

            // Initialize schema for user data database
            InitializeUserDataSchema(connection);

            var status = existed
                ? DatabaseProvisioningStatus.ReusedCopied
                : DatabaseProvisioningStatus.CreatedWritable;
            ReportProvision(new DatabaseProvisionedEvent(file, status, DateTimeOffset.UtcNow, nameof(OpenWritableDatabase)));

            return connection;
        }
        catch (Exception ex)
        {
            ReportProvision(new DatabaseProvisionedEvent(file, DatabaseProvisioningStatus.Failed, DateTimeOffset.UtcNow, nameof(OpenWritableDatabase), ex));
            throw;
        }
    }

    SQLiteConnection OpenBundledDatabase(DatabaseFile file)
    {
        if (file.Behavior != DatabaseBehavior.BundledReadOnly)
        {
            var ex = new InvalidOperationException($"'{file.Name}' is not a bundled read-only database. Behavior: {file.Behavior}");
            ReportProvision(new DatabaseProvisionedEvent(file, DatabaseProvisioningStatus.Failed, DateTimeOffset.UtcNow, nameof(OpenBundledDatabase), ex));
            throw ex;
        }

        // Try direct bundle access first (iOS, Desktop)
        var bundlePath = _bundledFileProvider.TryGetReadOnlyPath($"Data/{file.Name}");
        if (bundlePath != null)
        {
            try
            {
                var connection = new SQLiteConnection(bundlePath, SQLiteOpenFlags.ReadOnly);
                ReportProvision(new DatabaseProvisionedEvent(file, DatabaseProvisioningStatus.ReusedBundled, DateTimeOffset.UtcNow, nameof(OpenBundledDatabase)));
                return connection;
            }
            catch (Exception ex)
            {
                ReportProvision(new DatabaseProvisionedEvent(file, DatabaseProvisioningStatus.Failed, DateTimeOffset.UtcNow, nameof(OpenBundledDatabase), ex));
                throw;
            }
        }

        // Need to copy (Android, WASM)
        var localPath = IOPath.Combine(_bundledFileProvider.GetUserDataDirectory(), file.Name);
        if (NeedsCopy(file, localPath, out var isCorrupt))
        {
            try
            {
                // If file is corrupt, delete it first
                if (isCorrupt && IOFile.Exists(localPath))
                {
                    try
                    {
                        IOFile.Delete(localPath);
                        ReportProvision(new DatabaseProvisionedEvent(file, DatabaseProvisioningStatus.DeletedCorrupt, DateTimeOffset.UtcNow, nameof(OpenBundledDatabase)));
                    }
                    catch (Exception deleteEx)
                    {
                        ReportProvision(new DatabaseProvisionedEvent(file, DatabaseProvisioningStatus.FailedToDeleteCorrupt, DateTimeOffset.UtcNow, nameof(OpenBundledDatabase), deleteEx));
                    }
                }

                CopyBundledDatabase(file, localPath);
                ReportProvision(new DatabaseProvisionedEvent(file, DatabaseProvisioningStatus.CopiedFromBundle, DateTimeOffset.UtcNow, nameof(CopyBundledDatabase)));
            }
            catch (Exception ex)
            {
                var isDiskSpaceError = IsDiskSpaceError(ex);
                var status = isDiskSpaceError
                    ? DatabaseProvisioningStatus.FailedDiskSpace
                    : DatabaseProvisioningStatus.Failed;
                ReportProvision(new DatabaseProvisionedEvent(file, status, DateTimeOffset.UtcNow, nameof(CopyBundledDatabase), ex));
                throw new IOException($"Failed to copy {file.Name} — possible disk space or permissions issue", ex);
            }
        }
        else
        {
            ReportProvision(new DatabaseProvisionedEvent(file, DatabaseProvisioningStatus.ReusedCopied, DateTimeOffset.UtcNow, nameof(OpenBundledDatabase)));
        }

        try
        {
            return new SQLiteConnection(localPath, SQLiteOpenFlags.ReadOnly);
        }
        catch (Exception ex)
        {
            ReportProvision(new DatabaseProvisionedEvent(file, DatabaseProvisioningStatus.Failed, DateTimeOffset.UtcNow, nameof(OpenBundledDatabase), ex));
            throw;
        }
    }

    void CopyBundledDatabase(DatabaseFile file, string destinationPath)
    {
        // Atomic copy pattern: write to temp file first, then move to final location.
        // This prevents partial writes if the operation is interrupted.
        var tempPath = destinationPath + ".tmp";

        try
        {
            // Synchronous copy - we block on the async stream open
            using (var sourceStream = _bundledFileProvider.OpenReadStreamAsync($"Data/{file.Name}").GetAwaiter().GetResult())
            using (var destinationStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
            {
                var buffer = new byte[8192];
                int bytesRead;
                while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    destinationStream.Write(buffer, 0, bytesRead);
                }
                destinationStream.Flush();
            }

            // Only move to final location after complete success
            if (IOFile.Exists(destinationPath))
                IOFile.Delete(destinationPath);

            IOFile.Move(tempPath, destinationPath);
        }
        catch
        {
            // Clean up temp file on failure
            if (IOFile.Exists(tempPath))
            {
                try { IOFile.Delete(tempPath); }
                catch { /* Ignore cleanup errors */ }
            }
            throw;
        }
    }

    bool NeedsCopy(DatabaseFile file, string targetPath, out bool isCorrupt)
    {
#if DEBUG
        // Always copy in debug mode to ensure latest database
        isCorrupt = false;
        return true;
#else
        isCorrupt = false;

        if (!IOFile.Exists(targetPath))
            return true;

        try
        {
            using var connection = new SQLiteConnection(targetPath, SQLiteOpenFlags.ReadOnly);

            // Check database integrity
            var integrity = connection.ExecuteScalar<string>("PRAGMA integrity_check;");
            if (!string.Equals(integrity, "ok", StringComparison.OrdinalIgnoreCase))
            {
                isCorrupt = true;
                var ex = new InvalidDataException($"Integrity check failed: {integrity}");
                ReportProvision(new DatabaseProvisionedEvent(file, DatabaseProvisioningStatus.CorruptionDetected, DateTimeOffset.UtcNow, nameof(NeedsCopy), ex));
                return true;
            }

            // Check if bundle version is newer
            var previousVersion = connection.ExecuteScalar<int>("PRAGMA user_version;");
            return previousVersion < BundleVersion;
        }
        catch (Exception ex)
        {
            isCorrupt = true;
            ReportProvision(new DatabaseProvisionedEvent(file, DatabaseProvisioningStatus.CorruptionDetected, DateTimeOffset.UtcNow, nameof(NeedsCopy), ex));
            return true;
        }
#endif
    }

    static void InitializeUserDataSchema(SQLiteConnection connection)
    {
        // Create noun-specific tables
        connection.CreateTable<NounsFormMastery>();
        connection.CreateTable<NounsCombinationDifficulty>();
        connection.CreateTable<NounsPracticeHistory>();

        // Create verb-specific tables
        connection.CreateTable<VerbsFormMastery>();
        connection.CreateTable<VerbsCombinationDifficulty>();
        connection.CreateTable<VerbsPracticeHistory>();

        // Create shared tables
        connection.CreateTable<UserSetting>();
        connection.CreateTable<DailyProgress>();

        // Create indices for efficient querying
        connection.Execute("CREATE INDEX IF NOT EXISTS idx_nouns_mastery_level ON nouns_form_mastery(mastery_level)");
        connection.Execute("CREATE INDEX IF NOT EXISTS idx_verbs_mastery_level ON verbs_form_mastery(mastery_level)");
        connection.Execute("CREATE INDEX IF NOT EXISTS idx_nouns_difficulty ON nouns_combination_difficulty(difficulty_score DESC)");
        connection.Execute("CREATE INDEX IF NOT EXISTS idx_verbs_difficulty ON verbs_combination_difficulty(difficulty_score DESC)");
        connection.Execute("CREATE INDEX IF NOT EXISTS idx_nouns_history_date ON nouns_practice_history(practiced_utc DESC)");
        connection.Execute("CREATE INDEX IF NOT EXISTS idx_verbs_history_date ON verbs_practice_history(practiced_utc DESC)");
    }

    static SQLiteConnection CreateEmptyDatabase(DatabaseFile file)
    {
        var inMemoryConnection = new SQLiteConnection(":memory:");

        // Initialize the appropriate schema based on a file type
        if (file.Equals(DatabaseFile.UserData))
        {
            InitializeUserDataSchema(inMemoryConnection);
        }
        // Pali database tables would be empty, but that's acceptable for graceful degradation

        return inMemoryConnection;
    }

    static bool IsDiskSpaceError(Exception ex)
    {
        // Check for disk space error HResults
        // 0x80070070 = ERROR_DISK_FULL
        // 0x80070027 = ERROR_HANDLE_DISK_FULL
        const int errorDiskFull = unchecked((int)0x80070070);
        const int errorHandleDiskFull = unchecked((int)0x80070027);

        if (ex is IOException { HResult: errorDiskFull or errorHandleDiskFull })
            return true;

        // Also check inner exceptions
        return ex.InnerException != null && IsDiskSpaceError(ex.InnerException);
    }

    void ReportProvision(DatabaseProvisionedEvent e)
    {
        _provisionLog.Add(e);

        // Log to debug output
        var summary = e.Exception != null
            ? $"[DB] {e.Database.Name} [{e.Source}] FAILED: {e.Exception.Message}"
            : $"[DB] {e.Database.Name} [{e.Source}] -> {e.Status}";

        System.Diagnostics.Debug.WriteLine(summary);
    }

    static void LogTableCounts(SQLiteConnection db, string dbName)
    {
        try
        {
            // Get list of tables
            var tables = db.Query<TableInfo>("SELECT name FROM sqlite_master WHERE type='table' ORDER BY name");
            System.Diagnostics.Debug.WriteLine($"[DB] {dbName} tables: {string.Join(", ", tables.Select(t => t.Name))}");

            // Log counts for known tables
            foreach (var table in tables)
            {
                try
                {
                    var count = db.ExecuteScalar<int>($"SELECT COUNT(*) FROM \"{table.Name}\"");
                    System.Diagnostics.Debug.WriteLine($"[DB] {dbName}.{table.Name}: {count} rows");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DB] {dbName}.{table.Name}: ERROR - {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DB] {dbName} LogTableCounts FAILED: {ex.Message}");
        }
    }

    class TableInfo
    {
        public string Name { get; set; } = "";
    }
}
