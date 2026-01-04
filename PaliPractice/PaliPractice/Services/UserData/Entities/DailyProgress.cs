using SQLite;

namespace PaliPractice.Services.UserData.Entities;

/// <summary>
/// Tracks daily practice progress for goal completion.
/// </summary>
[Table("daily_progress")]
public class DailyProgress
{
    /// <summary>
    /// Hour at which a new "practice day" begins (local time).
    /// Using 5am ensures late-night practice counts toward "today's" goal.
    /// </summary>
    public const int DayStartHour = 5;

    /// <summary>
    /// Date in YYYY-MM-DD format (based on logical day, not calendar day).
    /// </summary>
    [PrimaryKey]
    [Column("date")]
    public string Date { get; set; } = string.Empty;

    [Column("declensions_completed")]
    public int DeclensionsCompleted { get; set; }

    [Column("conjugations_completed")]
    public int ConjugationsCompleted { get; set; }

    /// <summary>
    /// Gets the current "logical day" date key.
    /// The day resets at 5am local time, so practice between midnight and 5am
    /// counts toward the previous day's goal.
    /// </summary>
    public static string TodayKey
    {
        get
        {
            var now = DateTime.Now;
            // If before 5am, consider it still "yesterday"
            var logicalDate = now.Hour < DayStartHour ? now.Date.AddDays(-1) : now.Date;
            return logicalDate.ToString("yyyy-MM-dd");
        }
    }

    /// <summary>
    /// Create a new progress record for today.
    /// </summary>
    public static DailyProgress CreateForToday() => new()
    {
        Date = TodayKey,
        DeclensionsCompleted = 0,
        ConjugationsCompleted = 0
    };

    /// <summary>
    /// Gets the UTC timestamp for the start of the current "logical day".
    /// The day resets at 5am local time, so this returns the UTC equivalent
    /// of the current logical day's 5am boundary.
    /// </summary>
    public static DateTime TodayStartUtc
    {
        get
        {
            var now = DateTime.Now;
            // If before 5am, the logical day started yesterday at 5am
            var logicalDayStart = now.Hour < DayStartHour
                ? now.Date.AddDays(-1).AddHours(DayStartHour)
                : now.Date.AddHours(DayStartHour);
            return logicalDayStart.ToUniversalTime();
        }
    }

    /// <summary>
    /// Gets the UTC timestamp for the start of a logical day N days ago.
    /// </summary>
    public static DateTime GetDayStartUtc(int daysAgo)
    {
        var now = DateTime.Now;
        var logicalDayStart = now.Hour < DayStartHour
            ? now.Date.AddDays(-1 - daysAgo).AddHours(DayStartHour)
            : now.Date.AddDays(-daysAgo).AddHours(DayStartHour);
        return logicalDayStart.ToUniversalTime();
    }
}
