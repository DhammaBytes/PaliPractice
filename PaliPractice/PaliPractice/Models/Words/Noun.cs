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

    /// <summary>
    /// Raw pattern string from the database (e.g., "a masc").
    /// </summary>
    [Column("pattern")]
    public string RawPattern { get; set; } = string.Empty;

    /// <summary>
    /// Typed pattern enum value parsed from RawPattern.
    /// </summary>
    [Ignore]
    // TODO: assign once
    public NounPattern Pattern => NounPatternHelper.Parse(RawPattern);

    /// <summary>
    /// Whether this noun uses an irregular declension pattern.
    /// Irregular patterns return full forms instead of endings.
    /// </summary>
    [Ignore]
    public bool Irregular => Pattern.IsIrregular();

    /// <summary>
    /// Whether this noun is plural-only (lacks singular forms).
    /// True "pluralia tantum" nouns should not be asked for singular declensions.
    /// </summary>
    [Ignore]
    public bool PluralOnly => Pattern.IsPluralOnly();

    /// <summary>
    /// Display details for this noun. Lazy-loaded when showing flashcards.
    /// </summary>
    [Ignore]
    public IWordDetails? Details { get; set; }
}
