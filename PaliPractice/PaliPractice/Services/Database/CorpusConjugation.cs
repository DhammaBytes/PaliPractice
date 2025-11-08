using SQLite;

namespace PaliPractice.Services.Database;

/// <summary>
/// Represents a verb conjugation form that appears in the Pali Tipitaka corpus.
/// Only corpus-attested forms are stored (not theoretical forms).
/// Internal to the database service layer.
/// </summary>
[Table("corpus_conjugations")]
internal class CorpusConjugation
{
    [Column("verb_id")]
    public int VerbId { get; set; }

    [Column("person")]
    public int Person { get; set; }

    [Column("tense")]
    public int Tense { get; set; }

    [Column("voice")]
    public int Voice { get; set; }

    [Column("ending_index")]
    public int EndingIndex { get; set; }
}
