using SQLite;

namespace PaliPractice.Services.Database.Entities;

/// <summary>
/// Represents a verb conjugation form that appears in the Pali Tipitaka corpus.
/// Only corpus-attested forms are stored (not theoretical forms).
/// Internal to the database service layer.
/// </summary>
[Table("verbs_corpus_forms")]
class VerbCorpusForm
{
    [Column("headword_id")]
    public int HeadwordId { get; set; }

    /// <summary>
    /// Encoded form ID: lemma_id(5) + tense(1) + person(1) + number(1) + voice(1) + ending_index(1)
    /// </summary>
    [Column("form_id")]
    public long FormId { get; set; }
    [Column("form")]
    public string Form { get; set; } = "";
}
