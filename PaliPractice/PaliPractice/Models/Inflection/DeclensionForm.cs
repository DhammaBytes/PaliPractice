namespace PaliPractice.Models.Inflection;

/// <summary>
/// Represents a single inflected form variant within a declension group.
/// Immutable value type for efficient storage and comparison.
/// </summary>
public readonly record struct DeclensionForm(
    // The inflected form (stem + ending).
    string Form,

    // The ending only (for UI highlighting).
    string Ending,

    // Which ending variant this is (0, 1, 2...) when multiple endings are valid.
    int EndingIndex,

    // Whether this specific form appears in the Pali Tipitaka corpus.
    bool InCorpus
);
