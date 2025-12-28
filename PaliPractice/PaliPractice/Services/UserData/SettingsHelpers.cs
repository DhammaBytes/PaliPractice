using PaliPractice.Models;

namespace PaliPractice.Services.UserData;

/// <summary>
/// Helper methods for converting between enum collections and CSV strings for settings storage.
/// All settings are stored as comma-separated integers (e.g., "1,2,3").
/// </summary>
public static class SettingsHelpers
{
    /// <summary>
    /// Converts an enum collection to a CSV string of integer values.
    /// Example: [Case.Nominative, Case.Accusative] → "1,2"
    /// </summary>
    public static string ToCsv<T>(IEnumerable<T> values) where T : struct, Enum
        => string.Join(",", values.Select(v => Convert.ToInt32(v)));

    /// <summary>
    /// Parses a CSV string of integer values into a list of enum values.
    /// Example: "1,2,3" → [Case.Nominative, Case.Accusative, Case.Instrumental]
    /// </summary>
    public static List<T> FromCsv<T>(string csv) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(csv))
            return [];

        return csv.Split(',')
            .Select(s => s.Trim())
            .Where(s => int.TryParse(s, out _))
            .Select(s => (T)Enum.ToObject(typeof(T), int.Parse(s)))
            .ToList();
    }

    /// <summary>
    /// Parses a CSV string of integer values into a HashSet of enum values.
    /// Useful for O(1) membership checks.
    /// </summary>
    public static HashSet<T> FromCsvSet<T>(string csv) where T : struct, Enum
        => FromCsv<T>(csv).ToHashSet();

    /// <summary>
    /// Checks if an enum value is present in a CSV string.
    /// </summary>
    public static bool Contains<T>(string csv, T value) where T : struct, Enum
        => FromCsvSet<T>(csv).Contains(value);

    /// <summary>
    /// Checks if the numbers CSV includes Singular.
    /// </summary>
    public static bool IncludesSingular(string numbersCsv)
        => Contains(numbersCsv, Number.Singular);

    /// <summary>
    /// Checks if the numbers CSV includes Plural.
    /// </summary>
    public static bool IncludesPlural(string numbersCsv)
        => Contains(numbersCsv, Number.Plural);

    /// <summary>
    /// Checks if the voices CSV includes Normal (active).
    /// </summary>
    public static bool IncludesNormal(string voicesCsv)
        => Contains(voicesCsv, Voice.Normal);

    /// <summary>
    /// Checks if the voices CSV includes Reflexive.
    /// </summary>
    public static bool IncludesReflexive(string voicesCsv)
        => Contains(voicesCsv, Voice.Reflexive);
}
