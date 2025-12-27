using SQLite;

namespace PaliPractice.Models.Words;

/// <summary>
/// Display details for verbs. Lazy-loaded when showing flashcards.
/// Uses same id as verbs table (1:1 relationship with all variants preserved).
/// </summary>
[Table("verbs_details")]
public class VerbDetails : IWordDetails
{
    /// <summary>
    /// DPD headword ID (same as verbs.id for 1:1 join).
    /// </summary>
    [PrimaryKey]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    /// Our stable lemma ID for grouping variants.
    /// </summary>
    [Column("lemma_id")]
    public int LemmaId { get; set; }

    [Column("word")]
    public string Variant { get; set; } = string.Empty;

    [Column("type")]
    public string VerbType { get; set; } = string.Empty;

    [Column("trans")]
    public string Trans { get; set; } = string.Empty;

    [Column("meaning")]
    public string Meaning { get; set; } = string.Empty;

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

    /// <summary>
    /// Gets the transitivity as an enum value.
    /// </summary>
    [Ignore]
    public Transitivity Transitivity => Trans switch
    {
        "trans" => Transitivity.Transitive,
        "intrans" => Transitivity.Intransitive,
        "ditrans" => Transitivity.Ditransitive,
        _ => Transitivity.None
    };
}
