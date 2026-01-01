namespace PaliPractice.Models.Inflection;

/// <summary>
/// Represents a grouped noun declension for a specific case/number/gender combination.
/// Contains 1-N possible form variants (usually 1-3).
///
/// FormId encoding (9 digits):
///   LLLLL_C_G_N_E  where L=lemmaId, C=case, G=gender, N=number, E=endingId
///   Example: lemma 12345, accusative(2), masculine(1), plural(2), ending 0
///            → 123452120
///
/// EndingId=0 represents the combination itself (used for SRS tracking).
/// EndingId=1+ represents specific form variants within the combination.
/// </summary>
public class Declension
{
    /// <summary>
    /// Divisor to extract lemmaId from formId: formId / GrammarDivisor = lemmaId.
    /// FormId format: LLLLL_CGNE (5 digits lemma + 4 digits grammar).
    /// </summary>
    public const int GrammarDivisor = 10_000;

    /// <summary>
    /// Stable ID for the lemma group (10001-69999 for nouns).
    /// </summary>
    public int LemmaId { get; init; }

    /// <summary>
    /// The grammatical case.
    /// </summary>
    public Case Case { get; init; }

    /// <summary>
    /// Masculine, Neuter, or Feminine.
    /// </summary>
    public Gender Gender { get; init; }

    /// <summary>
    /// Singular or Plural.
    /// </summary>
    public Number Number { get; init; }

    /// <summary>
    /// Unique ID for this declension combination (EndingId=0).
    /// Format: lemmaId(5) + case(1) + gender(1) + number(1) + 0
    /// Example: 10789_3_1_2_0 → 107893120
    /// </summary>
    public int FormId => ResolveId(LemmaId, Case, Gender, Number, 0);

    /// <summary>
    /// All possible form variants for this declension (1-N forms).
    /// </summary>
    public IReadOnlyList<DeclensionForm> Forms { get; init; } = [];

    /// <summary>
    /// Computes a unique FormId from declension components.
    /// </summary>
    /// <param name="endingId">1-based ending ID. Use 0 for combination reference.</param>
    public static int ResolveId(int lemmaId, Case @case, Gender gender, Number number, int endingId)
    {
        return
            lemmaId * 10_000 +
            (int)@case * 1_000 +
            (int)gender * 100 +
            (int)number * 10 +
            endingId;
    }

    /// <summary>
    /// Parses a FormId back into its component parts.
    /// </summary>
    public static (int LemmaId, Case Case, Gender Gender, Number Number, int EndingId) ParseId(int formId)
    {
        return (
            formId / 10_000,
            (Case)(formId % 10_000 / 1_000),
            (Gender)(formId % 1_000 / 100),
            (Number)(formId % 100 / 10),
            formId % 10
        );
    }

    // DPD-style abbreviations for combo keys (public for UI reuse)
    public static IReadOnlyDictionary<Case, string> CaseAbbreviations { get; } = new Dictionary<Case, string>
    {
        [Case.Nominative] = "nom",
        [Case.Accusative] = "acc",
        [Case.Instrumental] = "instr",
        [Case.Dative] = "dat",
        [Case.Ablative] = "abl",
        [Case.Genitive] = "gen",
        [Case.Locative] = "loc",
        [Case.Vocative] = "voc"
    };

    public static IReadOnlyDictionary<Gender, string> GenderAbbreviations { get; } = new Dictionary<Gender, string>
    {
        [Gender.Masculine] = NounEndings.MascAbbrev,
        [Gender.Feminine] = NounEndings.FemAbbrev,
        [Gender.Neuter] = NounEndings.NeutAbbrev
    };

    public static IReadOnlyDictionary<Number, string> NumberAbbreviations { get; } = new Dictionary<Number, string>
    {
        [Number.Singular] = "sg",
        [Number.Plural] = "pl"
    };

    /// <summary>
    /// Generate DPD-style combo key for a declension (e.g., "nom_masc_sg").
    /// </summary>
    public static string ComboKey(Case @case, Gender gender, Number number)
    {
        var c = CaseAbbreviations.GetValueOrDefault(@case, @case.ToString().ToLowerInvariant());
        var g = GenderAbbreviations.GetValueOrDefault(gender, gender.ToString().ToLowerInvariant());
        var n = NumberAbbreviations.GetValueOrDefault(number, number.ToString().ToLowerInvariant());
        return $"{c}_{g}_{n}";
    }

    /// <summary>
    /// Generate combo key from a FormId.
    /// </summary>
    public static string ComboKeyFromId(int formId)
    {
        var parsed = ParseId(formId);
        return ComboKey(parsed.Case, parsed.Gender, parsed.Number);
    }

    /// <summary>
    /// Returns the primary attested form:
    /// - First, try EndingId=1 (first/default ending) if InCorpus
    /// - Otherwise, return first InCorpus form (any EndingId)
    /// - Null if no InCorpus forms exist
    /// </summary>
    public DeclensionForm? Primary
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
