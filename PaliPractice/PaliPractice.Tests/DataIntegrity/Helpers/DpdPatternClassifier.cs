using Microsoft.Data.Sqlite;
using PaliPractice.Models.Inflection;

namespace PaliPractice.Tests.DataIntegrity.Helpers;

/// <summary>
/// Queries DPD's inflection_templates table to get authoritative pattern classification.
/// This is the source of truth for what patterns are irregular vs regular/variant.
/// </summary>
/// <remarks>
/// <para>
/// The inflection_templates table has a "like" column that indicates:
/// - 'irreg' = truly irregular pattern (forms must be read from database)
/// - an example word (e.g., 'dhamma', 'bhavati') = regular or variant pattern
/// </para>
/// <para>
/// This allows tests to detect when DPD changes pattern classifications,
/// rather than relying on hardcoded sets that can become stale.
/// </para>
/// </remarks>
public class DpdPatternClassifier : IDisposable
{
    readonly SqliteConnection _connection;
    bool _disposed;

    // Cache the classification on first access
    Dictionary<string, string>? _patternLikeMap;

    public DpdPatternClassifier(string? dpdDbPath = null)
    {
        var path = dpdDbPath ?? DpdWordLoader.DefaultDpdDbPath;
        _connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly");
        _connection.Open();
    }

    /// <summary>
    /// Get all pattern → like mappings from inflection_templates.
    /// </summary>
    Dictionary<string, string> GetPatternLikeMap()
    {
        if (_patternLikeMap != null)
            return _patternLikeMap;

        _patternLikeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        const string sql = "SELECT pattern, \"like\" FROM inflection_templates";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var pattern = reader.GetString(0);
            var like = reader.GetString(1);
            _patternLikeMap[pattern] = like;
        }

        return _patternLikeMap;
    }

    /// <summary>
    /// Returns true if the pattern is marked as 'irreg' in DPD.
    /// </summary>
    public bool IsIrregular(string pattern)
    {
        var map = GetPatternLikeMap();
        return map.TryGetValue(pattern, out var like) &&
               like.Equals("irreg", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Get all irregular noun patterns (masc/fem/nt) according to DPD.
    /// </summary>
    public HashSet<string> GetIrregularNounPatterns()
    {
        var map = GetPatternLikeMap();
        return map
            .Where(kvp => kvp.Value.Equals("irreg", StringComparison.OrdinalIgnoreCase))
            .Where(kvp => IsNounPattern(kvp.Key))
            .Select(kvp => kvp.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Get all irregular present tense verb patterns (" pr") according to DPD.
    /// Note: The app only works with present tense, so we filter to " pr" patterns only.
    /// </summary>
    public HashSet<string> GetIrregularVerbPatterns()
    {
        var map = GetPatternLikeMap();
        return map
            .Where(kvp => kvp.Value.Equals("irreg", StringComparison.OrdinalIgnoreCase))
            .Where(kvp => IsVerbPattern(kvp.Key))
            .Select(kvp => kvp.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Get all regular/variant noun patterns (non-irreg) according to DPD.
    /// </summary>
    public HashSet<string> GetRegularNounPatterns()
    {
        var map = GetPatternLikeMap();
        return map
            .Where(kvp => !kvp.Value.Equals("irreg", StringComparison.OrdinalIgnoreCase))
            .Where(kvp => IsNounPattern(kvp.Key))
            .Select(kvp => kvp.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Get all regular present tense verb patterns (" pr", non-irreg) according to DPD.
    /// Note: The app only works with present tense, so we filter to " pr" patterns only.
    /// </summary>
    public HashSet<string> GetRegularVerbPatterns()
    {
        var map = GetPatternLikeMap();
        return map
            .Where(kvp => !kvp.Value.Equals("irreg", StringComparison.OrdinalIgnoreCase))
            .Where(kvp => IsVerbPattern(kvp.Key))
            .Select(kvp => kvp.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Get the "like" value for a pattern (the example word or 'irreg').
    /// Returns null if pattern not found.
    /// </summary>
    public string? GetLikeValue(string pattern)
    {
        var map = GetPatternLikeMap();
        return map.TryGetValue(pattern, out var like) ? like : null;
    }

    /// <summary>
    /// Check if a pattern string represents a noun (masc/fem/nt).
    /// </summary>
    static bool IsNounPattern(string pattern) => NounEndings.IsNounPattern(pattern);

    /// <summary>
    /// Check if a pattern string represents a present tense verb (" pr").
    /// Note: The app only works with present tense verbs, not other tenses.
    /// </summary>
    static bool IsVerbPattern(string pattern) =>
        pattern.Contains(" pr");

    public void Dispose()
    {
        if (!_disposed)
        {
            _connection?.Dispose();
            _disposed = true;
        }
    }
}
