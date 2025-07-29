using SQLite;

namespace PaliPractice.Models;

[Table("declensions")]
public class Declension
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    
    [Column("headword_id")]
    public int HeadwordId { get; set; }
    
    [Column("form")]
    public string Form { get; set; } = string.Empty;
    
    [Column("case_name")]
    public string? CaseName { get; set; }
    
    [Column("number")]
    public string? Number { get; set; }
    
    [Column("gender")]
    public string? Gender { get; set; }
}