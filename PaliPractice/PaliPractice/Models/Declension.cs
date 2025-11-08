namespace PaliPractice.Models;

/// <summary>
/// Represents a generated noun declension form.
/// This is a plain object created at runtime, not mapped to a database table.
/// </summary>
public class Declension
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
    /// The grammatical case.
    /// </summary>
    public NounCase CaseName { get; set; }

    /// <summary>
    /// Singular or Plural.
    /// </summary>
    public Number Number { get; set; }

    /// <summary>
    /// Masculine, Neuter, or Feminine.
    /// </summary>
    public Gender Gender { get; set; }

    /// <summary>
    /// Which ending variant this is (0, 1, 2...) when multiple endings are valid.
    /// </summary>
    public int EndingIndex { get; set; }

    /// <summary>
    /// Whether this specific form appears in the Pali Tipitaka corpus.
    /// </summary>
    public bool InCorpus { get; set; }
}
