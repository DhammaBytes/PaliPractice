namespace PaliPractice.Services.Database;

/// <summary>
/// Defines how a database file should be provisioned.
/// </summary>
public enum DatabaseBehavior
{
    /// <summary>
    /// User-writable database created in local app data.
    /// </summary>
    CreatedUserWritable,

    /// <summary>
    /// Read-only database bundled with the app.
    /// On some platforms (iOS, Desktop) can be read directly from bundle.
    /// On others (Android, WASM) must be copied to local storage first.
    /// </summary>
    BundledReadOnly
}

/// <summary>
/// Represents a database file with its name and provisioning behavior.
/// </summary>
public readonly record struct DatabaseFile
{
    public string Name { get; }
    public DatabaseBehavior Behavior { get; }

    DatabaseFile(string name, DatabaseBehavior behavior)
    {
        Name = name;
        Behavior = behavior;
    }

    /// <summary>
    /// Pali database with nouns, verbs, patterns, and corpus data.
    /// Bundled read-only - extracted from DPD and embedded in app.
    /// </summary>
    public static readonly DatabaseFile Pali = new("pali.db", DatabaseBehavior.BundledReadOnly);

    /// <summary>
    /// User data database for mastery tracking, settings, and history.
    /// Created on first use in user's local app data folder.
    /// </summary>
    public static readonly DatabaseFile UserData = new("practice.db", DatabaseBehavior.CreatedUserWritable);

    public override string ToString() => Name;
}
