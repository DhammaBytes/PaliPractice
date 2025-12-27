namespace PaliPractice.Models.Inflection;

/// <summary>
/// Irregular noun declension patterns.
/// These are word-specific patterns that don't follow regular rules.
/// Note: These patterns return full inflected forms (not just endings) because
/// irregular nouns have stem changes that can't be represented as simple suffixes.
/// </summary>
public static partial class NounEndings
{
    #region Irregular patterns

    /// <summary>rāja masc - irregular (king)</summary>
    static string[] GetRaja_Masc(Case nounCase, Number number)
    {
        return (nounCase, number) switch
        {
            (Case.Nominative, Number.Singular) => ["rājā"],
            (Case.Nominative, Number.Plural) => ["rājāno"],
            (Case.Accusative, Number.Singular) => ["rājaṃ", "rājānaṃ"],
            (Case.Accusative, Number.Plural) => ["rājāno", "rāje"],
            (Case.Instrumental, Number.Singular) => ["raññā", "rājinā", "rājena"],
            (Case.Instrumental, Number.Plural) => ["rājubhi", "rājūbhi", "rājūhi", "rājehi"],
            (Case.Dative, Number.Singular) => ["rañño", "rājino"],
            (Case.Dative, Number.Plural) => ["raññaṃ", "rājānaṃ", "rājūnaṃ"],
            (Case.Ablative, Number.Singular) => ["raññā", "rājato", "rājamhā", "rājasmā"],
            (Case.Ablative, Number.Plural) => ["rājubhi", "rājūhi", "rājehi"],
            (Case.Genitive, Number.Singular) => ["rañño", "rājassa", "rājino"],
            (Case.Genitive, Number.Plural) => ["raññaṃ", "rājānaṃ", "rājūnaṃ"],
            (Case.Locative, Number.Singular) => ["raññe", "rājamhi", "rājasmiṃ", "rājini"],
            (Case.Locative, Number.Plural) => ["rājūsu", "rājesu"],
            (Case.Vocative, Number.Singular) => ["rāja", "rājā"],
            (Case.Vocative, Number.Plural) => ["rājāno"],
            _ => []
        };
    }

    /// <summary>brahma masc - irregular (Brahmā)</summary>
    static string[] GetBrahma_Masc(Case nounCase, Number number)
    {
        return (nounCase, number) switch
        {
            (Case.Nominative, Number.Singular) => ["brahmā"],
            (Case.Nominative, Number.Plural) => ["brahmā", "brahmāno"],
            (Case.Accusative, Number.Singular) => ["brahmaṃ", "brahmānaṃ"],
            (Case.Accusative, Number.Plural) => ["brahmāno"],
            (Case.Instrumental, Number.Singular) => ["brahmunā"],
            (Case.Instrumental, Number.Plural) => ["brahmūhi", "brahmehi"],
            (Case.Dative, Number.Singular) => ["brahmassa", "brahmuno"],
            (Case.Dative, Number.Plural) => ["brahmānaṃ", "brahmūnaṃ"],
            (Case.Ablative, Number.Singular) => ["brahmunā"],
            (Case.Ablative, Number.Plural) => ["brahmūhi", "brahmehi"],
            (Case.Genitive, Number.Singular) => ["brahmassa", "brahmuno"],
            (Case.Genitive, Number.Plural) => ["brahmānaṃ", "brahmūnaṃ"],
            (Case.Locative, Number.Singular) => ["brahmani", "brahmasmiṃ"],
            (Case.Locative, Number.Plural) => ["brahmesu"],
            (Case.Vocative, Number.Singular) => ["brahma", "brahme"],
            (Case.Vocative, Number.Plural) => ["brahmāno"],
            _ => []
        };
    }

    /// <summary>kamma nt - irregular (action/deed)</summary>
    static string[] GetKamma_Nt(Case nounCase, Number number)
    {
        return (nounCase, number) switch
        {
            (Case.Nominative, Number.Singular) => ["kamma", "kammaṃ", "kamme"],
            (Case.Nominative, Number.Plural) => ["kammā", "kammāni"],
            (Case.Accusative, Number.Singular) => ["kamma", "kammaṃ"],
            (Case.Accusative, Number.Plural) => ["kammāni", "kamme"],
            (Case.Instrumental, Number.Singular) => ["kammanā", "kammunā", "kammena"],
            (Case.Instrumental, Number.Plural) => ["kammehi"],
            (Case.Dative, Number.Singular) => ["kammassa", "kammāya", "kammuno"],
            (Case.Dative, Number.Plural) => ["kammānaṃ"],
            (Case.Ablative, Number.Singular) => ["kammamhā", "kammasmā", "kammunā"],
            (Case.Ablative, Number.Plural) => ["kammehi"],
            (Case.Genitive, Number.Singular) => ["kammassa", "kammuno"],
            (Case.Genitive, Number.Plural) => ["kammānaṃ"],
            (Case.Locative, Number.Singular) => ["kammani", "kammamhi", "kammasmiṃ", "kamme"],
            (Case.Locative, Number.Plural) => ["kammesu"],
            (Case.Vocative, Number.Singular) => ["kamma"],
            (Case.Vocative, Number.Plural) => ["kammā", "kammāni"],
            _ => []
        };
    }

    #endregion
}
