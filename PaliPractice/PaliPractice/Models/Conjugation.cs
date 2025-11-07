using SQLite;

namespace PaliPractice.Models;

[Table("conjugations")]
public class Conjugation
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Column("verb_id")]
    public int VerbId { get; set; }

    [Column("form")]
    public string Form { get; set; } = string.Empty;

    [Column("person")]
    public Person Person { get; set; }

    [Column("tense")]
    public Tense Tense { get; set; }

    [Column("mood")]
    public Mood Mood { get; set; }

    [Column("voice")]
    public Voice Voice { get; set; }

    [Column("in_corpus")]
    public bool InCorpus { get; set; }
}
