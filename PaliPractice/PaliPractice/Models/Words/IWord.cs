namespace PaliPractice.Models.Words;

/// <summary>
/// Common interface for both Noun and Verb entities.
/// Provides access to shared properties while allowing concrete implementations
/// to maintain their unique properties (Gender for Noun, Pos for Verb).
/// </summary>
public interface IWord
{
    int Id { get; set; }
    int EbtCount { get; set; }
    int LemmaId { get; set; }

    /// <summary>
    /// The lemma (dictionary form) of this word, e.g., "aññāti".
    /// </summary>
    string Lemma { get; set; }

    /// <summary>
    /// The variant identifier within the lemma group, e.g., "1.1" or empty string.
    /// Combined with Lemma gives the full DPD identifier like "aññāti 1.1".
    /// </summary>
    string Variant { get; set; }

    string? Stem { get; set; }
    string Pattern { get; set; }

    /// <summary>
    /// Whether this word uses an irregular inflection pattern.
    /// Irregular patterns return full forms from GetEndings() instead of
    /// endings that should be appended to the stem.
    /// </summary>
    bool Irregular { get; }

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
