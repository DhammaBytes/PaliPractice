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
    public string? Person { get; set; }
    
    [Column("tense")]
    public string? Tense { get; set; }
    
    [Column("mood")]
    public string? Mood { get; set; }
    
    [Column("voice")]
    public string? Voice { get; set; }
}