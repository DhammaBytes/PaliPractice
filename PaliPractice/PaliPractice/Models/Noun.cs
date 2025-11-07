using SQLite;

namespace PaliPractice.Models;

[Table("nouns")]
public class Noun : IWord
{
    [PrimaryKey]
    public int Id { get; set; }

    [Column("ebt_count")]
    public int EbtCount { get; set; }

    [Column("lemma")]
    public string Lemma { get; set; } = string.Empty;

    [Column("lemma_clean")]
    public string LemmaClean { get; set; } = string.Empty;

    [Column("gender")]
    public Gender Gender { get; set; }

    [Column("stem")]
    public string? Stem { get; set; }

    [Column("pattern")]
    public string? Pattern { get; set; }

    [Column("family_root")]
    public string FamilyRoot { get; set; } = string.Empty;

    [Column("meaning")]
    public string? Meaning { get; set; }

    [Column("plus_case")]
    public string PlusCase { get; set; } = string.Empty;

    [Column("source_1")]
    public string Source1 { get; set; } = string.Empty;

    [Column("sutta_1")]
    public string Sutta1 { get; set; } = string.Empty;

    [Column("example_1")]
    public string Example1 { get; set; } = string.Empty;

    [Column("source_2")]
    public string Source2 { get; set; } = string.Empty;

    [Column("sutta_2")]
    public string Sutta2 { get; set; } = string.Empty;

    [Column("example_2")]
    public string Example2 { get; set; } = string.Empty;
}
