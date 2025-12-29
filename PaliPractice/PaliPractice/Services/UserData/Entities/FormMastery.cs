using SQLite;

namespace PaliPractice.Services.UserData.Entities;

/// <summary>
/// Base class for form mastery tracking.
/// Tracks mastery level for inflected forms (EndingId=0 FormIds).
/// NextDue is calculated from LastPracticedUtc + cooldown based on MasteryLevel.
/// </summary>
public abstract class FormMasteryBase
{
    /// <summary>
    /// FormId with EndingId=0 (combination reference).
    /// Declension: 9-digit, Conjugation: 10-digit.
    /// </summary>
    [PrimaryKey]
    [Column("form_id")]
    public long FormId { get; set; }

    /// <summary>
    /// Mastery level 1-10. Default: 5.
    /// 1 = struggling (1 day cooldown), 10 = mastered (~8.5 months cooldown).
    /// </summary>
    [Column("mastery_level")]
    public int MasteryLevel { get; set; } = CooldownCalculator.DefaultLevel;

    /// <summary>
    /// Previous mastery level before last practice.
    /// Used to show progress direction in HistoryPage.
    /// </summary>
    [Column("previous_level")]
    public int PreviousLevel { get; set; } = CooldownCalculator.DefaultLevel;

    [Column("last_practiced_utc")]
    public DateTime LastPracticedUtc { get; set; }

    /// <summary>
    /// Calculated next due time based on LastPracticedUtc and MasteryLevel.
    /// </summary>
    [Ignore]
    public DateTime NextDueUtc => CooldownCalculator.CalculateNextDue(LastPracticedUtc, MasteryLevel);

    /// <summary>
    /// Whether this form is currently due for review.
    /// </summary>
    [Ignore]
    public bool IsDue => CooldownCalculator.IsDue(LastPracticedUtc, MasteryLevel);

    /// <summary>
    /// Whether the level improved since last practice.
    /// </summary>
    [Ignore]
    public bool IsImproved => MasteryLevel > PreviousLevel;
}

/// <summary>
/// Form mastery for noun declensions.
/// </summary>
[Table("nouns_form_mastery")]
public class NounsFormMastery : FormMasteryBase { }

/// <summary>
/// Form mastery for verb conjugations.
/// </summary>
[Table("verbs_form_mastery")]
public class VerbsFormMastery : FormMasteryBase { }
