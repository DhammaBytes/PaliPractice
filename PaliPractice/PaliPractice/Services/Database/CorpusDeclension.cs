using SQLite;

namespace PaliPractice.Services.Database;

/// <summary>
/// Represents a noun declension form that appears in the Pali Tipitaka corpus.
/// Only corpus-attested forms are stored (not theoretical forms).
/// Internal to the database service layer.
/// </summary>
[Table("corpus_declensions")]
class CorpusDeclension
{
    [Column("noun_id")]
    public int NounId { get; set; }

    [Column("case_name")]
    public int CaseName { get; set; }

    [Column("number")]
    public int Number { get; set; }

    [Column("gender")]
    public int Gender { get; set; }

    [Column("ending_index")]
    public int EndingIndex { get; set; }
}
