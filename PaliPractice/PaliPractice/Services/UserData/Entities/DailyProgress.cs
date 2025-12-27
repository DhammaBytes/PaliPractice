using SQLite;

namespace PaliPractice.Services.UserData.Entities;

/// <summary>
/// Tracks daily practice progress for goal completion.
/// </summary>
[Table("daily_progress")]
public class DailyProgress
{
    /// <summary>
    /// Date in YYYY-MM-DD format.
    /// </summary>
    [PrimaryKey]
    [Column("date")]
    public string Date { get; set; } = string.Empty;

    [Column("declensions_completed")]
    public int DeclensionsCompleted { get; set; }

    [Column("conjugations_completed")]
    public int ConjugationsCompleted { get; set; }

    /// <summary>
    /// Get today's date key.
    /// </summary>
    public static string TodayKey => DateTime.UtcNow.ToString("yyyy-MM-dd");

    /// <summary>
    /// Create a new progress record for today.
    /// </summary>
    public static DailyProgress CreateForToday() => new()
    {
        Date = TodayKey,
        DeclensionsCompleted = 0,
        ConjugationsCompleted = 0
    };
}
