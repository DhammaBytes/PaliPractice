using SQLite;

namespace PaliPractice.Models;

[Table("conjugations")]
public class Conjugation
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Column("headword_id")]
    public int HeadwordId { get; set; }

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
}
