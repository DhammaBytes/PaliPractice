namespace PaliPractice.Models.Inflection;

/// <summary>
/// Represents a grouped noun declension for a specific case/number/gender combination.
/// Contains 1-N possible form variants (usually 1-3).
/// </summary>
public class Declension
{
    /// <summary>
    /// The grammatical case.
    /// </summary>
    public NounCase CaseName { get; init; }

    /// <summary>
    /// Singular or Plural.
    /// </summary>
    public Number Number { get; init; }

    /// <summary>
    /// Masculine, Neuter, or Feminine.
    /// </summary>
    public Gender Gender { get; init; }

    /// <summary>
    /// All possible form variants for this declension (1-N forms).
    /// </summary>
    public IReadOnlyList<DeclensionForm> Forms { get; init; } = [];

    /// <summary>
    /// Returns the primary attested form:
    /// - First, try EndingIndex=0 if InCorpus
    /// - Otherwise, return first InCorpus form (any index)
    /// - Null if no InCorpus forms exist
    /// </summary>
    public DeclensionForm? Primary
    {
        get
        {
            // Try EndingIndex=0 first
            foreach (var form in Forms)
            {
                if (form.EndingIndex == 0 && form.InCorpus)
                    return form;
            }

            // Fall back to first InCorpus form
            foreach (var form in Forms)
            {
                if (form.InCorpus)
                    return form;
            }

            return null;
        }
    }
}
