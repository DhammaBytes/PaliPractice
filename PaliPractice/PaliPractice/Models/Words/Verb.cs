using PaliPractice.Models.Inflection;
using SQLite;

namespace PaliPractice.Models.Words;

/// <summary>
/// Slim verb model for queue building and inflection generation.
/// Details (meaning, examples) are lazy-loaded via VerbDetails when needed for display.
/// </summary>
[Table("verbs")]
public class Verb : IWord
{
    [PrimaryKey]
    public int Id { get; set; }

    [Column("ebt_count")]
    public int EbtCount { get; set; }

    [Column("lemma_id")]
    public int LemmaId { get; set; }

    /// <summary>
    /// The lemma (dictionary form) of this verb, e.g., "bhavati".
    /// </summary>
    [Column("lemma")]
    public string Lemma { get; set; } = string.Empty;

    [Column("stem")]
    public string? Stem { get; set; }

    [Column("pattern")]
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Whether this verb uses an irregular conjugation pattern.
    /// Irregular patterns return full forms instead of endings.
    /// </summary>
    [Ignore]
    public bool Irregular => VerbPatterns.IsIrregular(Pattern);

    /// <summary>
    /// Display details for this verb. Lazy-loaded when showing flashcards.
    /// </summary>
    [Ignore]
    public IWordDetails? Details { get; set; }
}
