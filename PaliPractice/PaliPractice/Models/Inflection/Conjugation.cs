namespace PaliPractice.Models.Inflection;

/// <summary>
/// Represents a grouped verb conjugation for a specific person/number/tense/reflexive combination.
/// Contains 1-N possible form variants (usually 1-3).
/// </summary>
public class Conjugation
{
    /// <summary>
    /// Stable ID for the lemma group (70001-99999 for verbs).
    /// </summary>
    public int LemmaId { get; init; }

    /// <summary>
    /// Tense (includes traditional moods): Present, Imperative, Optative, Future, Aorist.
    /// </summary>
    public Tense Tense { get; init; }

    /// <summary>
    /// First, Second, or Third person.
    /// </summary>
    public Person Person { get; init; }

    /// <summary>
    /// Singular or Plural.
    /// </summary>
    public Number Number { get; init; }

    /// <summary>
    /// Whether this is a reflexive (middle voice) form.
    /// </summary>
    public bool Reflexive { get; init; }

    /// <summary>
    /// Unique ID for this conjugation combination (EndingId=0).
    /// Format: lemmaId(5) + tense(1) + person(1) + number(1) + reflexive(1) + 0
    /// Example: 70683_2_3_1_0_0 → 7068323100
    /// </summary>
    public long FormId => ResolveId(LemmaId, Tense, Person, Number, Reflexive, 0);

    /// <summary>
    /// All possible form variants for this conjugation (1-N forms).
    /// </summary>
    public IReadOnlyList<ConjugationForm> Forms { get; init; } = [];

    /// <summary>
    /// Computes a unique FormId from conjugation components.
    /// </summary>
    /// <param name="endingId">1-based ending ID. Use 0 for combination reference.</param>
    public static long ResolveId(int lemmaId, Tense tense, Person person, Number number, bool reflexive, int endingId)
    {
        return
            (long)lemmaId * 100_000 +
            (int)tense * 10_000 +
            (int)person * 1_000 +
            (int)number * 100 +
            (reflexive ? 1 : 0) * 10 +
            endingId;
    }

    /// <summary>
    /// Parses a FormId back into its component parts.
    /// </summary>
    public static (int LemmaId, Tense Tense, Person Person, Number Number, bool Reflexive, int EndingId) ParseId(long formId)
    {
        return (
            (int)(formId / 100_000),
            (Tense)(formId % 100_000 / 10_000),
            (Person)(formId % 10_000 / 1_000),
            (Number)(formId % 1_000 / 100),
            (formId % 100 / 10) == 1,
            (int)(formId % 10)
        );
    }

    /// <summary>
    /// Returns the primary attested form:
    /// - First, try EndingId=1 (first/default ending) if InCorpus
    /// - Otherwise, return first InCorpus form (any EndingId)
    /// - Null if no InCorpus forms exist
    /// </summary>
    public ConjugationForm? Primary
    {
        get
        {
            // Try EndingId=1 first (the default ending)
            foreach (var form in Forms)
            {
                if (form is { EndingId: 1, InCorpus: true })
                    return form;
            }

            // Fall back to the first InCorpus form
            foreach (var form in Forms)
            {
                if (form.InCorpus)
                    return form;
            }

            return null;
        }
    }
}
