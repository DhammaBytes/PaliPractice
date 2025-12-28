using SQLite;

namespace PaliPractice.Services.Database;

/// <summary>
/// Represents a verb conjugation form that appears in the Pali Tipitaka corpus.
/// Only corpus-attested forms are stored (not theoretical forms).
/// Internal to the database service layer.
/// </summary>
[Table("verbs_corpus_forms")]
class VerbCorpusForm
{
    /// <summary>
    /// Encoded form ID: lemma_id(5) + tense(1) + person(1) + number(1) + voice(1) + ending_index(1)
    /// </summary>
    [PrimaryKey]
    [Column("form_id")]
    public long FormId { get; set; }
}
