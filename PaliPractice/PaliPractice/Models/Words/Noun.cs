using PaliPractice.Models.Inflection;
using SQLite;

namespace PaliPractice.Models.Words;

/// <summary>
/// Slim noun model for queue building and inflection generation.
/// Details (meaning, examples) are lazy-loaded via NounDetails when needed for display.
/// </summary>
[Table("nouns")]
public class Noun : IWord
{
    [PrimaryKey]
    public int Id { get; set; }

    [Column("ebt_count")]
    public int EbtCount { get; set; }

    [Column("lemma_id")]
    public int LemmaId { get; set; }

    /// <summary>
    /// The lemma (dictionary form) of this noun, e.g., "dhamma".
    /// </summary>
    [Column("lemma")]
    public string Lemma { get; set; } = string.Empty;

    [Column("gender")]
    public Gender Gender { get; set; }

    [Column("stem")]
    public string? Stem { get; set; }

    [Column("pattern")]
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Whether this noun uses an irregular declension pattern.
    /// Irregular patterns return full forms instead of endings.
    /// </summary>
    [Ignore]
    public bool Irregular => NounPatterns.IsIrregular(Pattern);

    /// <summary>
    /// Display details for this noun. Lazy-loaded when showing flashcards.
    /// </summary>
    [Ignore]
    public IWordDetails? Details { get; set; }
}
