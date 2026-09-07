using SQLite;

namespace PaliPractice.Services.Database.Entities;

/// <summary>
/// Represents an irregular verb form stored in the database.
/// Irregular forms are pre-computed full forms (not stem+ending) generated from the pinned DPD template.
/// Internal to the database service layer.
/// </summary>
[Table("verbs_irregular_forms")]
class VerbIrregularForm
{
    [Column("headword_id")]
    public int HeadwordId { get; set; }

    /// <summary>
    /// Encoded form ID: lemma_id(5) + tense(1) + person(1) + number(1) + reflexive(1) + ending_index(1)
    /// </summary>
    [Column("form_id")]
    public long FormId { get; set; }

    /// <summary>
    /// The full conjugated form (not just the ending).
    /// </summary>
    [Column("form")]
    public string Form { get; set; } = "";
}
