namespace PaliPractice.Services.UserData;

/// <summary>
/// Calculates cooldown intervals for the spaced repetition system.
/// Formula: CooldownHours = BaseHours * Multiplier^(Level - 1)
///
/// Optimized for a dead language with longer retention periods:
///
/// Level | Cooldown  | Description
/// ------|-----------|------------
///   1   | 1 day     | Struggling
///   2   | 1.9 days  | Very weak
///   3   | 3.4 days  | Weak
///   4   | 6.3 days  | Below average
///   5   | 12 days   | Default for new forms
///   6   | 22 days   | Average
///   7   | 40 days   | Good
///   8   | 74 days   | Strong
///   9   | 137 days  | Very strong
///  10   | 254 days  | Mastered (~8.5 months)
/// </summary>
public static class CooldownCalculator
{
    public const double BaseHours = 24.0;  // 1 day minimum
    public const double Multiplier = 1.85; // Reaches ~254 days at level 10
    public const int MinLevel = 1;
    public const int MaxLevel = 10;
    public const int DefaultLevel = 5;

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
    /// Easy: +1 level (max 10)
    /// Hard: -2 levels (min 1)
    /// </summary>
    public static int AdjustLevel(int currentLevel, bool wasEasy)
    {
        if (wasEasy)
            return Math.Min(MaxLevel, currentLevel + 1);
        else
            return Math.Max(MinLevel, currentLevel - 2);
    }
}
