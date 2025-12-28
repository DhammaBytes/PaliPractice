using SQLite;

namespace PaliPractice.Services.Database.Entities;

/// <summary>
/// Represents a noun declension form that appears in the Pali Tipitaka corpus.
/// Only corpus-attested forms are stored (not theoretical forms).
/// Internal to the database service layer.
/// </summary>
[Table("nouns_corpus_forms")]
class NounCorpusForm
{
    /// <summary>
    /// Encoded form ID: lemma_id(5) + case(1) + gender(1) + number(1) + ending_index(1)
    /// </summary>
    [PrimaryKey]
    [Column("form_id")]
    public int FormId { get; set; }
}
