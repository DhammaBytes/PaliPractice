using SQLite;

namespace PaliPractice.Services.UserData.Entities;

public enum HistorySnapshotOrigin
{
    Unknown = 0,
    Practiced = 1,
    ReconstructedV11 = 2
}

/// <summary>
/// Common interface for practice history records.
/// Used for type-agnostic display in HistoryPage.
/// </summary>
public interface IPracticeHistory
{
    int Id { get; }
    long FormId { get; }
    /// <summary>
    /// The inflected form text captured at practice time, or recovered for older records.
    /// </summary>
    string FormText { get; set; }
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
    /// Stored at practice time so dictionary updates cannot rewrite history.
    /// </summary>
    [Column("form_text")]
    public string FormText { get; set; } = "";

    [Column("lemma_text")]
    public string LemmaText { get; set; } = "";

    [Column("grammar_text")]
    public string GrammarText { get; set; } = "";

    [Column("snapshot_origin")]
    public HistorySnapshotOrigin SnapshotOrigin { get; set; }

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
    /// Capped at 100% (level 11 = retired shows as 100%).
    /// </summary>
    [Ignore]
    public int NewLevelPercent => Math.Min(NewLevel, 10) * 10;
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
