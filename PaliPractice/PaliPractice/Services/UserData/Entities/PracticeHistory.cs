using SQLite;

namespace PaliPractice.Services.UserData.Entities;

/// <summary>
/// Records each practice session for showing progress in HistoryPage.
/// Tracks the form, level change, and timestamp.
/// </summary>
[Table("practice_history")]
public class PracticeHistory
{
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

    [Column("form_id")]
    [Indexed]
    public long FormId { get; set; }

    [Column("practice_type")]
    [Indexed]
    public PracticeType PracticeType { get; set; }

    /// <summary>
    /// The actual inflected form text (for display in history).
    /// </summary>
    [Column("form_text")]
    public string FormText { get; set; } = "";

    /// <summary>
    /// Mastery level before this practice.
    /// </summary>
    [Column("old_level")]
    public int OldLevel { get; set; }

    /// <summary>
    /// Mastery level after this practice.
    /// </summary>
    [Column("new_level")]
    public int NewLevel { get; set; }

    [Column("practiced_utc")]
    [Indexed]
    public DateTime PracticedUtc { get; set; }

    /// <summary>
    /// Whether the level improved (NewLevel > OldLevel).
    /// </summary>
    [Ignore]
    public bool IsImproved => NewLevel > OldLevel;

    /// <summary>
    /// New level as percentage (0-100) for progress bar display.
    /// </summary>
    [Ignore]
    public int NewLevelPercent => NewLevel * 10;
}
