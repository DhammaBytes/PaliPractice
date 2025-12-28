namespace PaliPractice.Services.Database.Providers;

/// <summary>
/// Platform abstraction for accessing bundled files.
/// Different platforms have different ways to access app-bundled assets.
/// </summary>
public interface IBundledFileProvider
{
    /// <summary>
    /// If the platform can expose the bundled file through a read-only path
    /// that SQLite can open directly, return it. Otherwise, return null.
    /// </summary>
    /// <param name="relativePath">Relative path within the app bundle (e.g., "Data/training.db")</param>
    /// <returns>Absolute path to the file, or null if copying is required</returns>
    string? TryGetReadOnlyPath(string relativePath);

    /// <summary>
    /// Open a readable stream for the bundled file.
    /// Used when copying is required (Android, WASM).
    /// </summary>
    /// <param name="relativePath">Relative path within the app bundle</param>
    /// <returns>Stream for reading the file</returns>
    Task<Stream> OpenReadStreamAsync(string relativePath);

    /// <summary>
    /// Returns the platform-specific directory path where the app
    /// can store user files and databases.
    /// </summary>
    string GetUserDataDirectory();
}
