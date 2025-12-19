namespace PaliPractice.Models;

/// <summary>
/// Represents a generated verb conjugation form.
/// This is a plain object created at runtime, not mapped to a database table.
/// </summary>
public class Conjugation
{
    /// <summary>
    /// The inflected form (stem + ending).
    /// </summary>
    public string Form { get; set; } = string.Empty;

    /// <summary>
    /// The ending only (for UI highlighting).
    /// </summary>
    public string Ending { get; set; } = string.Empty;

    /// <summary>
    /// First, Second, or Third person.
    /// </summary>
    public Person Person { get; set; }

    /// <summary>
    /// Singular or Plural.
    /// </summary>
    public Number Number { get; set; }

    /// <summary>
    /// Tense (includes traditional moods): Present, Imperative, Optative, Future, Aorist.
    /// </summary>
    public Tense Tense { get; set; }

    /// <summary>
    /// Active, Reflexive, Passive, Causative.
    /// </summary>
    public Voice Voice { get; set; }

    /// <summary>
    /// Which ending variant this is (0, 1, 2...) when multiple endings are valid.
    /// </summary>
    public int EndingIndex { get; set; }

    /// <summary>
    /// Whether this specific form appears in the Pali Tipitaka corpus.
    /// </summary>
    public bool InCorpus { get; set; }
}
