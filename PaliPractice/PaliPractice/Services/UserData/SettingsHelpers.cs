using PaliPractice.Models;

namespace PaliPractice.Services.UserData;

/// <summary>
/// Helper methods for settings: CSV conversion, validation, and index-based dropdown conversions.
/// </summary>
public static class SettingsHelpers
{
    #region Validation

    /// <summary>
    /// Minimum allowed daily goal value.
    /// </summary>
    public const int DailyGoalMin = 10;

    /// <summary>
    /// Maximum allowed daily goal value.
    /// </summary>
    public const int DailyGoalMax = 500;

    /// <summary>
    /// Validates and clamps a daily goal value to allowed range.
    /// </summary>
    public static int ValidateDailyGoal(int goal)
        => Math.Clamp(goal, DailyGoalMin, DailyGoalMax);

    /// <summary>
    /// Validates and clamps a range (min/max pair) to allowed bounds.
    /// Ensures min is less than max.
    /// </summary>
    public static (int min, int max) ValidateRange(int min, int max, int minAllowed, int maxAllowed)
    {
        // Clamp to bounds
        min = Math.Max(min, minAllowed);
        max = Math.Min(max, maxAllowed);

        // Ensure max is at least minAllowed + 1
        if (max < minAllowed + 1)
            max = minAllowed + 1;

        // Ensure min < max
        if (min >= max)
            min = max - 1;

        return (min, max);
    }

    #endregion

    #region Number Index Conversion

    /// <summary>
    /// Labels for the number dropdown. Index: 0=both, 1=singular only, 2=plural only.
    /// </summary>
    public static readonly string[] NumberOptions = ["Singular & Plural", "Singular only", "Plural only"];

    /// <summary>
    /// Converts dropdown index to CSV for storage.
    /// </summary>
    public static string NumberIndexToCsv(int index) => index switch
    {
        1 => ToCsv([Number.Singular]),
        2 => ToCsv([Number.Plural]),
        _ => ToCsv([Number.Singular, Number.Plural])
    };

    /// <summary>
    /// Converts CSV to dropdown index.
    /// </summary>
    public static int NumberIndexFromCsv(string csv)
    {
        var singular = IncludesSingular(csv);
        var plural = IncludesPlural(csv);
        return (singular, plural) switch
        {
            (true, true) => 0,
            (true, false) => 1,
            (false, true) => 2,
            _ => 0
        };
    }

    #endregion

    #region CSV Conversion

    /// <summary>
    /// Converts an enum collection to a CSV string of integer values.
    /// Example: [Case.Nominative, Case.Accusative] → "1,2"
    /// </summary>
    public static string ToCsv<T>(IEnumerable<T> values) where T : struct, Enum
        => string.Join(",", values.Select(v => Convert.ToInt32(v)));

    /// <summary>
    /// Parses a CSV string of integer values into a list of enum values.
    /// Filters out None (default) and undefined enum values.
    /// Example: "1,2,3" → [Case.Nominative, Case.Accusative, Case.Instrumental]
    /// </summary>
    public static List<T> FromCsv<T>(string csv) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(csv))
            return [];

        var defaultValue = default(T);
        return csv.Split(',')
            .Select(s => s.Trim())
            .Where(s => int.TryParse(s, out _))
            .Select(s => (T)Enum.ToObject(typeof(T), int.Parse(s)))
            .Where(v => Enum.IsDefined(v) && !v.Equals(defaultValue))
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
        => Contains(voicesCsv, Voice.Active);

    /// <summary>
    /// Checks if the voices CSV includes Reflexive.
    /// </summary>
    public static bool IncludesReflexive(string voicesCsv)
        => Contains(voicesCsv, Voice.Reflexive);

    #endregion
}
