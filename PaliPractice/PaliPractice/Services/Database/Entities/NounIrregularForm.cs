using SQLite;

namespace PaliPractice.Services.Database.Entities;

/// <summary>
/// Represents an irregular noun form stored in the database.
/// Irregular forms are pre-computed full forms (not stem+ending) generated from the pinned DPD template.
/// Internal to the database service layer.
/// </summary>
[Table("nouns_irregular_forms")]
class NounIrregularForm
{
    [Column("headword_id")]
    public int HeadwordId { get; set; }

    /// <summary>
    /// Encoded form ID: lemma_id(5) + case(1) + gender(1) + number(1) + ending_index(1)
    /// </summary>
    [Column("form_id")]
    public int FormId { get; set; }

    /// <summary>
    /// The full inflected form (not just the ending).
    /// </summary>
    [Column("form")]
    public string Form { get; set; } = "";
}
