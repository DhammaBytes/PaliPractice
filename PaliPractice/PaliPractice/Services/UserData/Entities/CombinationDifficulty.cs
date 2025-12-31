using PaliPractice.Models.Inflection;
using SQLite;

namespace PaliPractice.Services.UserData.Entities;

/// <summary>
/// Base class for tracking difficulty of grammatical combinations.
/// Uses DPD-style key-based storage and exponential moving average scoring.
/// </summary>
public abstract class CombinationDifficultyBase
{
    /// <summary>
    /// DPD-style combination key (e.g., "nom_masc_sg", "pr_1st_sg_reflx").
    /// </summary>
    [PrimaryKey]
    [Column("combo_key")]
    public string ComboKey { get; set; } = "";

    /// <summary>
    /// Difficulty score 0.0 (easy) to 1.0 (hard).
    /// Updated via exponential moving average after each practice.
    /// </summary>
    [Column("difficulty_score")]
    public double DifficultyScore { get; set; } = 0.5;

    [Column("total_attempts")]
    public int TotalAttempts { get; set; }

    [Column("last_updated_utc")]
    public DateTime LastUpdatedUtc { get; set; }
}

/// <summary>
/// Difficulty tracking for noun declension combinations.
/// </summary>
[Table("nouns_combination_difficulty")]
public class NounsCombinationDifficulty : CombinationDifficultyBase
{
    /// <summary>
    /// Create a new difficulty record for a declension combination.
    /// </summary>
    public static NounsCombinationDifficulty Create(Case @case, Gender gender, Number number)
    {
        return new NounsCombinationDifficulty
        {
            ComboKey = Declension.ComboKey(@case, gender, number),
            LastUpdatedUtc = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Difficulty tracking for verb conjugation combinations.
/// </summary>
[Table("verbs_combination_difficulty")]
public class VerbsCombinationDifficulty : CombinationDifficultyBase
{
    /// <summary>
    /// Create a new difficulty record for a conjugation combination.
    /// </summary>
    public static VerbsCombinationDifficulty Create(Tense tense, Person person, Number number, bool reflexive)
    {
        var voice = reflexive ? Voice.Reflexive : Voice.Active;
        return new VerbsCombinationDifficulty
        {
            ComboKey = Conjugation.ComboKey(tense, person, number, voice),
            LastUpdatedUtc = DateTime.UtcNow
        };
    }
}
