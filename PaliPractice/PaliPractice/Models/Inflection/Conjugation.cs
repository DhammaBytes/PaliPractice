namespace PaliPractice.Models.Inflection;

/// <summary>
/// Represents a grouped verb conjugation for a specific person/number/tense/voice combination.
/// Contains 1-N possible form variants (usually 1-3).
///
/// FormId encoding (10 digits):
///   LLLLL_T_P_N_V_E  where L=lemmaId, T=tense, P=person, N=number, V=voice, E=endingId
///   Example: lemma 70123, present(1), 3rd(3), singular(1), normal(1), ending 0
///            → 7012313110
///
/// EndingId=0 represents the combination itself (used for SRS tracking).
/// EndingId=1+ represents specific form variants within the combination.
/// </summary>
public class Conjugation
{
    /// <summary>
    /// Divisor to extract lemmaId from formId: formId / LemmaDivisor = lemmaId.
    /// FormId format: LLLLL_TPNVE (5 digits lemma + 5 digits grammar).
    /// </summary>
    public const long LemmaDivisor = 100_000;

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
    /// Voice: Normal (active) or Reflexive (middle).
    /// </summary>
    public Voice Voice { get; init; }

    /// <summary>
    /// Unique ID for this conjugation combination (EndingId=0).
    /// Format: lemmaId(5) + tense(1) + person(1) + number(1) + voice(1) + 0
    /// Example: 70683_2_3_1_1_0 → 7068323110
    /// </summary>
    public long FormId => ResolveId(LemmaId, Tense, Person, Number, Voice, 0);

    /// <summary>
    /// All possible form variants for this conjugation (1-N forms).
    /// </summary>
    public IReadOnlyList<ConjugationForm> Forms { get; init; } = [];

    /// <summary>
    /// Computes a unique FormId from conjugation components.
    /// </summary>
    /// <param name="endingId">1-based ending ID. Use 0 for combination reference.</param>
    public static long ResolveId(int lemmaId, Tense tense, Person person, Number number, Voice voice, int endingId)
    {
        return
            (long)lemmaId * LemmaDivisor +
            (int)tense * 10_000 +
            (int)person * 1_000 +
            (int)number * 100 +
            (int)voice * 10 +
            endingId;
    }

    /// <summary>
    /// Parses a FormId back into its component parts.
    /// </summary>
    public static (int LemmaId, Tense Tense, Person Person, Number Number, Voice Voice, int EndingId) ParseId(long formId)
    {
        return (
            (int)(formId / LemmaDivisor),
            (Tense)(formId % LemmaDivisor / 10_000),
            (Person)(formId % 10_000 / 1_000),
            (Number)(formId % 1_000 / 100),
            (Voice)(formId % 100 / 10),
            (int)(formId % 10)
        );
    }

    // DPD-style abbreviations for combo keys (public for UI reuse)
    public static IReadOnlyDictionary<Tense, string> TenseAbbreviations { get; } = new Dictionary<Tense, string>
    {
        [Tense.Present] = "pr",
        [Tense.Imperative] = "imp",
        [Tense.Optative] = "opt",
        [Tense.Future] = "fut"
    };

    public static IReadOnlyDictionary<Person, string> PersonAbbreviations { get; } = new Dictionary<Person, string>
    {
        [Person.First] = "1st",
        [Person.Second] = "2nd",
        [Person.Third] = "3rd"
    };

    public static IReadOnlyDictionary<Number, string> NumberAbbreviations { get; } = new Dictionary<Number, string>
    {
        [Number.Singular] = "sg",
        [Number.Plural] = "pl"
    };

    public const string ReflexiveAbbrev = "reflx";

    /// <summary>
    /// Generate DPD-style combo key for a conjugation (e.g., "pr_1st_sg" or "opt_3rd_pl_reflx").
    /// </summary>
    public static string ComboKey(Tense tense, Person person, Number number, Voice voice)
    {
        var t = TenseAbbreviations.GetValueOrDefault(tense, tense.ToString().ToLowerInvariant());
        var p = PersonAbbreviations.GetValueOrDefault(person, person.ToString().ToLowerInvariant());
        var n = NumberAbbreviations.GetValueOrDefault(number, number.ToString().ToLowerInvariant());
        var key = $"{t}_{p}_{n}";
        return voice == Voice.Reflexive ? $"{key}_{ReflexiveAbbrev}" : key;
    }

    /// <summary>
    /// Generate combo key from a FormId.
    /// </summary>
    public static string ComboKeyFromId(long formId)
    {
        var parsed = ParseId(formId);
        return ComboKey(parsed.Tense, parsed.Person, parsed.Number, parsed.Voice);
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
