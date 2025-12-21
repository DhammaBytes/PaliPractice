namespace PaliPractice.Models;

/// <summary>
/// Represents a single inflected form variant within a declension group.
/// Immutable value type for efficient storage and comparison.
/// </summary>
public readonly record struct DeclensionForm(
    /// <summary>
    /// The inflected form (stem + ending).
    /// </summary>
    string Form,

    /// <summary>
    /// The ending only (for UI highlighting).
    /// </summary>
    string Ending,

    /// <summary>
    /// Which ending variant this is (0, 1, 2...) when multiple endings are valid.
    /// </summary>
    int EndingIndex,

    /// <summary>
    /// Whether this specific form appears in the Pali Tipitaka corpus.
    /// </summary>
    bool InCorpus
);
