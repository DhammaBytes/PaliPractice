namespace PaliPractice.Services.UserData;

/// <summary>
/// Calculates cooldown intervals for the spaced repetition system.
/// Formula: CooldownHours = BaseHours * Multiplier^(Level - 1)
///
/// Optimized for a dead language with longer retention periods:
///
/// Level | Cooldown  | Description
/// ------|-----------|------------
///   0   | n/a       | Unpracticed (never seen)
///   1   | 1 day     | Struggling
///   2   | 1.9 days  | Very weak
///   3   | 3.4 days  | Weak
///   4   | 6.3 days  | Default for first practice
///   5   | 12 days   | Below average
///   6   | 22 days   | Average
///   7   | 40 days   | Good
///   8   | 74 days   | Strong
///   9   | 137 days  | Very strong
///  10   | 254 days  | Mastered (~8.5 months)
///  11   | n/a       | Retired (excluded forever)
/// </summary>
public static class CooldownCalculator
{
    public const double BaseHours = 24.0;  // 1 day minimum
    public const double Multiplier = 1.85; // Reaches ~254 days at level 10
    public const int UnpracticedLevel = 0; // Display level for forms never practiced
    public const int MinLevel = 1;         // Minimum level for practiced forms
    public const int MaxLevel = 10;
    public const int RetiredLevel = 11;    // Forms at this level are excluded from practice forever
    public const int DefaultLevel = 4;     // Starting level when first practiced

    /// <summary>
    /// Pre-calculated cooldown hours for each level (index 0 = level 1).
    /// Used for efficient SQL queries without requiring POWER function.
    /// </summary>
    static readonly double[] CooldownHoursLookup = Enumerable.Range(MinLevel, MaxLevel)
        .Select(level => BaseHours * Math.Pow(Multiplier, level - 1))
        .ToArray();

    /// <summary>
    /// Get pre-calculated cooldown hours array for SQL query optimization.
    /// Returns array where index 0 = level 1 cooldown, index 9 = level 10 cooldown.
    /// </summary>
    public static double[] GetCooldownHoursLookup() => CooldownHoursLookup;

    /// <summary>
    /// Get cooldown duration in hours for a given level.
    /// </summary>
    public static double GetCooldownHours(int level)
    {
        var clampedLevel = Math.Clamp(level, MinLevel, MaxLevel);
        return BaseHours * Math.Pow(Multiplier, clampedLevel - 1);
    }

    /// <summary>
    /// Calculate next due time from a given practice time and mastery level.
    /// </summary>
    public static DateTime CalculateNextDue(DateTime lastPracticedUtc, int level)
    {
        return lastPracticedUtc.AddHours(GetCooldownHours(level));
    }

    /// <summary>
    /// Check if a form is due for review based on last practice time and level.
    /// </summary>
    public static bool IsDue(DateTime lastPracticedUtc, int level)
    {
        return DateTime.UtcNow >= CalculateNextDue(lastPracticedUtc, level);
    }

    /// <summary>
    /// Adjust level based on practice result.
    /// Easy: +1 level (max 11 = retired)
    /// Hard: -1 level (min 1)
    /// </summary>
    public static int AdjustLevel(int currentLevel, bool wasEasy)
    {
        if (wasEasy)
            return Math.Min(RetiredLevel, currentLevel + 1);
        else
            return Math.Max(MinLevel, currentLevel - 1);
    }
}
