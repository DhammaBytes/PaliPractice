namespace PaliPractice.Models.Inflection;

/// <summary>
/// Represents a single inflected form variant within a declension group.
/// Immutable value type for efficient storage and comparison.
/// </summary>
/// <param name="FormId">Unique ID: lemmaId(5) + case(1) + gender(1) + number(1) + endingId(1). Example: 107893122</param>
/// <param name="Form">The inflected form (stem + ending).</param>
/// <param name="Ending">The ending only (for UI highlighting).</param>
/// <param name="EndingId">Which ending variant this is (1, 2, 3...) when multiple endings are valid. Subtract 1 for array index.</param>
/// <param name="InCorpus">Whether this specific form appears in the Pali Tipitaka corpus.</param>
public readonly record struct DeclensionForm(
    int FormId,
    string Form,
    string Ending,
    int EndingId,
    bool InCorpus
);
