using SQLite;

namespace PaliPractice.Services.Database;

/// <summary>
/// Represents an irregular noun form stored in the database.
/// Irregular forms are pre-computed full forms (not stem+ending) parsed from DPD HTML.
/// Internal to the database service layer.
/// </summary>
[Table("nouns_irregular_forms")]
class NounIrregularForm
{
    /// <summary>
    /// Encoded form ID: lemma_id(5) + case(1) + gender(1) + number(1) + ending_index(1)
    /// </summary>
    [PrimaryKey]
    [Column("form_id")]
    public int FormId { get; set; }

    /// <summary>
    /// The full inflected form (not just the ending).
    /// </summary>
    [Column("form")]
    public string Form { get; set; } = "";
}
