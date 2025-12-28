using PaliPractice.Models.Inflection;
using SQLite;

namespace PaliPractice.Services.UserData.Entities;

/// <summary>
/// Meta-bucket tracking difficulty of grammatical combinations.
/// Uses DPD-style key-based storage (e.g., "nom_masc_sg", "pr_1st_sg_reflx").
/// </summary>
[Table("combination_difficulty")]
public class CombinationDifficulty
{
    /// <summary>
    /// DPD-style combination key (e.g., "nom_masc_sg", "pr_1st_sg_reflx").
    /// </summary>
    [PrimaryKey]
    [Column("combo_key")]
    public string ComboKey { get; set; } = "";

    [Column("practice_type")]
    [Indexed]
    public PracticeType PracticeType { get; set; }

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

    /// <summary>
    /// Create a new difficulty record for a declension combination.
    /// </summary>
    public static CombinationDifficulty ForDeclension(Case @case, Gender gender, Number number)
    {
        return new CombinationDifficulty
        {
            ComboKey = Declension.ComboKey(@case, gender, number),
            PracticeType = PracticeType.Declension,
            LastUpdatedUtc = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Create a new difficulty record for a conjugation combination.
    /// </summary>
    public static CombinationDifficulty ForConjugation(Tense tense, Person person, Number number, bool reflexive)
    {
        var voice = reflexive ? Voice.Reflexive : Voice.Normal;
        return new CombinationDifficulty
        {
            ComboKey = Conjugation.ComboKey(tense, person, number, voice),
            PracticeType = PracticeType.Conjugation,
            LastUpdatedUtc = DateTime.UtcNow
        };
    }
}
