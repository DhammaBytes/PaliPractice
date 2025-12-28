using SQLite;

namespace PaliPractice.Services.UserData.Entities;

/// <summary>
/// Common interface for practice history records.
/// Used for type-agnostic display in HistoryPage.
/// </summary>
public interface IPracticeHistory
{
    int Id { get; }
    long FormId { get; }
    string FormText { get; }
    int OldLevel { get; }
    int NewLevel { get; }
    DateTime PracticedUtc { get; }
    bool IsImproved { get; }
    int NewLevelPercent { get; }
}

/// <summary>
/// Base class for practice history records.
/// Records each practice session for showing progress in HistoryPage.
/// Tracks the form, level change, and timestamp.
/// </summary>
public abstract class PracticeHistoryBase : IPracticeHistory
{
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

    [Column("form_id")]
    [Indexed]
    public long FormId { get; set; }

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

/// <summary>
/// Practice history for noun declensions.
/// </summary>
[Table("nouns_practice_history")]
public class NounsPracticeHistory : PracticeHistoryBase { }

/// <summary>
/// Practice history for verb conjugations.
/// </summary>
[Table("verbs_practice_history")]
public class VerbsPracticeHistory : PracticeHistoryBase { }
