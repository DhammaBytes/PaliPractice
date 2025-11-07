using SQLite;

namespace PaliPractice.Models;

[Table("declensions")]
public class Declension
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Column("noun_id")]
    public int NounId { get; set; }

    [Column("form")]
    public string Form { get; set; } = string.Empty;

    [Column("case_name")]
    public NounCase CaseName { get; set; }

    [Column("number")]
    public Number Number { get; set; }

    [Column("gender")]
    public Gender Gender { get; set; }

    [Column("in_corpus")]
    public bool InCorpus { get; set; }
}
