using SQLite;

namespace PaliPractice.Models;

[Table("headwords")]
public class Headword
{
    [PrimaryKey]
    public int Id { get; set; }
    
    [Column("lemma_1")]
    public string Lemma1 { get; set; } = string.Empty;
    
    [Column("lemma_clean")]
    public string LemmaClean { get; set; } = string.Empty;
    
    [Column("pos")]
    public string Pos { get; set; } = string.Empty;
    
    [Column("type")]
    public string Type { get; set; } = string.Empty;
    
    [Column("stem")]
    public string? Stem { get; set; }
    
    [Column("pattern")]
    public string? Pattern { get; set; }
    
    [Column("meaning_1")]
    public string? Meaning1 { get; set; }
    
    [Column("ebt_count")]
    public int EbtCount { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}