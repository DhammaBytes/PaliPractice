using PaliPractice.Models.Inflection;
using SQLite;

namespace PaliPractice.Models.Words;

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

    /// <summary>
    /// The variant identifier within the lemma group, e.g., "1" or "1.1" or empty.
    /// </summary>
    [Column("word")]
    public string Variant { get; set; } = string.Empty;

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

    [Column("derived_from")]
    public string DerivedFrom { get; set; } = string.Empty;

    [Column("family_root")]
    public string FamilyRoot { get; set; } = string.Empty;

    [Column("meaning")]
    public string? Meaning { get; set; }

    [Column("plus_case")]
    public string PlusCase { get; set; } = string.Empty;

    [Column("source_1")]
    public string Source1 { get; set; } = string.Empty;

    [Column("sutta_1")]
    public string Sutta1 { get; set; } = string.Empty;

    [Column("example_1")]
    public string Example1 { get; set; } = string.Empty;

    [Column("source_2")]
    public string Source2 { get; set; } = string.Empty;

    [Column("sutta_2")]
    public string Sutta2 { get; set; } = string.Empty;

    [Column("example_2")]
    public string Example2 { get; set; } = string.Empty;
}
