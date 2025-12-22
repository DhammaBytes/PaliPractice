namespace PaliPractice.Models.Inflection;

/// <summary>
/// Represents a grouped verb conjugation for a specific person/number/tense/voice combination.
/// Contains 1-N possible form variants (usually 1-3).
/// </summary>
public class Conjugation
{
    /// <summary>
    /// First, Second, or Third person.
    /// </summary>
    public Person Person { get; init; }

    /// <summary>
    /// Singular or Plural.
    /// </summary>
    public Number Number { get; init; }

    /// <summary>
    /// Tense (includes traditional moods): Present, Imperative, Optative, Future, Aorist.
    /// </summary>
    public Tense Tense { get; init; }

    /// <summary>
    /// Active, Reflexive, Passive, Causative.
    /// </summary>
    public Voice Voice { get; init; }

    /// <summary>
    /// All possible form variants for this conjugation (1-N forms).
    /// </summary>
    public IReadOnlyList<ConjugationForm> Forms { get; init; } = [];

    /// <summary>
    /// Returns the primary attested form:
    /// - First, try EndingIndex=0 if InCorpus
    /// - Otherwise, return first InCorpus form (any index)
    /// - Null if no InCorpus forms exist
    /// </summary>
    public ConjugationForm? Primary
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
