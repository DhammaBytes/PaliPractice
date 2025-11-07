namespace PaliPractice.Models;

/// <summary>
/// Common interface for both Noun and Verb entities.
/// Provides access to shared properties while allowing concrete implementations
/// to maintain their unique properties (Gender for Noun, Pos for Verb).
/// </summary>
public interface IWord
{
    int Id { get; set; }
    int EbtCount { get; set; }
    string Lemma { get; set; }
    string LemmaClean { get; set; }
    string? Stem { get; set; }
    string? Pattern { get; set; }
    string FamilyRoot { get; set; }
    string? Meaning { get; set; }
    string PlusCase { get; set; }
    string Source1 { get; set; }
    string Sutta1 { get; set; }
    string Example1 { get; set; }
    string Source2 { get; set; }
    string Sutta2 { get; set; }
    string Example2 { get; set; }
}
