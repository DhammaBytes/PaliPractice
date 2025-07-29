using SQLite;

namespace PaliPractice.Models;

[Table("patterns")]
public class Pattern
{
    [PrimaryKey]
    [Column("pattern_name")]
    public string PatternName { get; set; } = string.Empty;
    
    [Column("like_word")]
    public string? LikeWord { get; set; }
    
    [Column("pos_category")]
    public string? PosCategory { get; set; }
    
    [Column("template_data")]
    public string? TemplateData { get; set; }
}