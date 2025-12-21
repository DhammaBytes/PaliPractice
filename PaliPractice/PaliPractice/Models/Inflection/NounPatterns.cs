namespace PaliPractice.Models.Inflection;

/// <summary>
/// Provides noun declension endings for common patterns.
/// Returns array of possible endings (some cases have multiple valid forms).
/// Covers top 12 patterns = 94% of all nouns.
/// </summary>
public static class NounPatterns
{
    /// <summary>
    /// Get declension endings for a noun pattern.
    /// </summary>
    /// <param name="pattern">Pattern name (e.g., "a masc", "ā fem")</param>
    /// <param name="nounCase">Grammatical case</param>
    /// <param name="number">Singular or plural</param>
    /// <returns>Array of endings. Empty array if pattern not supported.</returns>
    public static string[] GetEndings(string pattern, NounCase nounCase, Number number)
    {
        return pattern switch
        {
            "a masc" => GetA_Masc(nounCase, number),
            "a nt" => GetA_Nt(nounCase, number),
            "ā fem" => GetA_Fem(nounCase, number),
            "i masc" => GetI_Masc(nounCase, number),
            "i fem" => GetI_Fem(nounCase, number),
            "ī masc" => GetI_Long_Masc(nounCase, number),
            "ī fem" => GetI_Long_Fem(nounCase, number),
            "u masc" => GetU_Masc(nounCase, number),
            "u nt" => GetU_Nt(nounCase, number),
            "u fem" => GetU_Fem(nounCase, number),
            "as masc" => GetAs_Masc(nounCase, number),
            "ar masc" => GetAr_Masc(nounCase, number),
            _ => Array.Empty<string>()
        };
    }

    static string[] GetA_Masc(NounCase nounCase, Number number)
    {
        return (nounCase, number) switch
        {
            (NounCase.Nominative, Number.Singular) => ["o"],
            (NounCase.Nominative, Number.Plural) => ["ā", "āse"],
            (NounCase.Accusative, Number.Singular) => ["aṃ"],
            (NounCase.Accusative, Number.Plural) => ["e"],
            (NounCase.Instrumental, Number.Singular) => ["ā", "ena"],
            (NounCase.Instrumental, Number.Plural) => ["ebhi", "ehi"],
            (NounCase.Dative, Number.Singular) => ["assa", "āya"],
            (NounCase.Dative, Number.Plural) => ["ānaṃ"],
            (NounCase.Ablative, Number.Singular) => ["ato", "amhā", "asmā", "ā"],
            (NounCase.Ablative, Number.Plural) => ["ato", "ebhi", "ehi"],
            (NounCase.Genitive, Number.Singular) => ["assa"],
            (NounCase.Genitive, Number.Plural) => ["āna", "ānaṃ"],
            (NounCase.Locative, Number.Singular) => ["amhi", "asmiṃ", "e"],
            (NounCase.Locative, Number.Plural) => ["esu"],
            (NounCase.Vocative, Number.Singular) => ["a", "ā"],
            (NounCase.Vocative, Number.Plural) => ["ā"],
            _ => Array.Empty<string>()
        };
    }

    static string[] GetA_Nt(NounCase nounCase, Number number)
    {
        return (nounCase, number) switch
        {
            (NounCase.Nominative, Number.Singular) => ["aṃ"],
            (NounCase.Nominative, Number.Plural) => ["ā", "āni"],
            (NounCase.Accusative, Number.Singular) => ["aṃ"],
            (NounCase.Accusative, Number.Plural) => ["āni", "e"],
            (NounCase.Instrumental, Number.Singular) => ["ā", "ena"],
            (NounCase.Instrumental, Number.Plural) => ["ebhi", "ehi"],
            (NounCase.Dative, Number.Singular) => ["assa", "āya"],
            (NounCase.Dative, Number.Plural) => ["ānaṃ"],
            (NounCase.Ablative, Number.Singular) => ["ato", "amhā", "asmā", "ā"],
            (NounCase.Ablative, Number.Plural) => ["ebhi", "ehi"],
            (NounCase.Genitive, Number.Singular) => ["assa"],
            (NounCase.Genitive, Number.Plural) => ["āna", "ānaṃ"],
            (NounCase.Locative, Number.Singular) => ["amhi", "asmiṃ", "e"],
            (NounCase.Locative, Number.Plural) => ["esu"],
            (NounCase.Vocative, Number.Singular) => ["a", "aṃ", "ā"],
            (NounCase.Vocative, Number.Plural) => ["ā", "āni"],
            _ => Array.Empty<string>()
        };
    }

    static string[] GetA_Fem(NounCase nounCase, Number number)
    {
        return (nounCase, number) switch
        {
            (NounCase.Nominative, Number.Singular) => ["ā"],
            (NounCase.Nominative, Number.Plural) => ["ā", "āyo"],
            (NounCase.Accusative, Number.Singular) => ["aṃ"],
            (NounCase.Accusative, Number.Plural) => ["ā", "āyo"],
            (NounCase.Instrumental, Number.Singular) => ["ā", "āya"],
            (NounCase.Instrumental, Number.Plural) => ["ābhi", "āhi"],
            (NounCase.Dative, Number.Singular) => ["āya"],
            (NounCase.Dative, Number.Plural) => ["ānaṃ"],
            (NounCase.Ablative, Number.Singular) => ["ato", "āto", "āya"],
            (NounCase.Ablative, Number.Plural) => ["ābhi", "āhi"],
            (NounCase.Genitive, Number.Singular) => ["āya"],
            (NounCase.Genitive, Number.Plural) => ["ānaṃ"],
            (NounCase.Locative, Number.Singular) => ["āya", "āyaṃ"],
            (NounCase.Locative, Number.Plural) => ["āsu"],
            (NounCase.Vocative, Number.Singular) => ["a", "e"],
            (NounCase.Vocative, Number.Plural) => ["ā", "āyo"],
            _ => Array.Empty<string>()
        };
    }

    static string[] GetI_Masc(NounCase nounCase, Number number)
    {
        return (nounCase, number) switch
        {
            (NounCase.Nominative, Number.Singular) => ["i"],
            (NounCase.Nominative, Number.Plural) => ["ayo", "ī"],
            (NounCase.Accusative, Number.Singular) => ["iṃ"],
            (NounCase.Accusative, Number.Plural) => ["ayo", "ī"],
            (NounCase.Instrumental, Number.Singular) => ["inā"],
            (NounCase.Instrumental, Number.Plural) => ["ibhi", "ībhi", "īhi"],
            (NounCase.Dative, Number.Singular) => ["ino", "issa"],
            (NounCase.Dative, Number.Plural) => ["inaṃ", "īnaṃ"],
            (NounCase.Ablative, Number.Singular) => ["ito", "inā", "imhā", "ismā"],
            (NounCase.Ablative, Number.Plural) => ["ibhi", "ībhi", "īhi"],
            (NounCase.Genitive, Number.Singular) => ["ino", "issa"],
            (NounCase.Genitive, Number.Plural) => ["inaṃ", "īnaṃ"],
            (NounCase.Locative, Number.Singular) => ["imhi", "ismiṃ"],
            (NounCase.Locative, Number.Plural) => ["isu", "īsu"],
            (NounCase.Vocative, Number.Singular) => ["i"],
            (NounCase.Vocative, Number.Plural) => ["ayo", "ī"],
            _ => Array.Empty<string>()
        };
    }

    static string[] GetI_Fem(NounCase nounCase, Number number)
    {
        return (nounCase, number) switch
        {
            (NounCase.Nominative, Number.Singular) => ["i"],
            (NounCase.Nominative, Number.Plural) => ["iyo", "ī"],
            (NounCase.Accusative, Number.Singular) => ["iṃ"],
            (NounCase.Accusative, Number.Plural) => ["iyo", "ī"],
            (NounCase.Instrumental, Number.Singular) => ["iyā"],
            (NounCase.Instrumental, Number.Plural) => ["ibhi", "ībhi", "īhi"],
            (NounCase.Dative, Number.Singular) => ["iyā"],
            (NounCase.Dative, Number.Plural) => ["īnaṃ"],
            (NounCase.Ablative, Number.Singular) => ["ito", "iyā"],
            (NounCase.Ablative, Number.Plural) => ["ibhi", "ībhi", "īhi"],
            (NounCase.Genitive, Number.Singular) => ["iyā"],
            (NounCase.Genitive, Number.Plural) => ["īnaṃ"],
            (NounCase.Locative, Number.Singular) => ["iyaṃ", "iyā"],
            (NounCase.Locative, Number.Plural) => ["isu", "īsu"],
            (NounCase.Vocative, Number.Singular) => ["i"],
            (NounCase.Vocative, Number.Plural) => ["iyo", "ī"],
            _ => Array.Empty<string>()
        };
    }

    static string[] GetI_Long_Masc(NounCase nounCase, Number number)
    {
        return (nounCase, number) switch
        {
            (NounCase.Nominative, Number.Singular) => ["ī"],
            (NounCase.Nominative, Number.Plural) => ["ino", "ī"],
            (NounCase.Accusative, Number.Singular) => ["inaṃ", "iṃ"],
            (NounCase.Accusative, Number.Plural) => ["ino", "ī"],
            (NounCase.Instrumental, Number.Singular) => ["inā"],
            (NounCase.Instrumental, Number.Plural) => ["ibhi", "īhi"],
            (NounCase.Dative, Number.Singular) => ["ino", "issa"],
            (NounCase.Dative, Number.Plural) => ["inaṃ", "īnaṃ"],
            (NounCase.Ablative, Number.Singular) => ["inā", "imhā"],
            (NounCase.Ablative, Number.Plural) => ["ibhi", "īhi"],
            (NounCase.Genitive, Number.Singular) => ["ino", "issa"],
            (NounCase.Genitive, Number.Plural) => ["inaṃ", "īnaṃ"],
            (NounCase.Locative, Number.Singular) => ["ini", "imhi", "ismiṃ"],
            (NounCase.Locative, Number.Plural) => ["isu", "īsu"],
            (NounCase.Vocative, Number.Singular) => ["i", "ī"],
            (NounCase.Vocative, Number.Plural) => ["ino", "ī"],
            _ => Array.Empty<string>()
        };
    }

    static string[] GetI_Long_Fem(NounCase nounCase, Number number)
    {
        return (nounCase, number) switch
        {
            (NounCase.Nominative, Number.Singular) => ["ī"],
            (NounCase.Nominative, Number.Plural) => ["iyo", "ī"],
            (NounCase.Accusative, Number.Singular) => ["iṃ"],
            (NounCase.Accusative, Number.Plural) => ["iyo", "ī"],
            (NounCase.Instrumental, Number.Singular) => ["iyā"],
            (NounCase.Instrumental, Number.Plural) => ["ibhi", "ībhi", "īhi"],
            (NounCase.Dative, Number.Singular) => ["iyā"],
            (NounCase.Dative, Number.Plural) => ["īnaṃ"],
            (NounCase.Ablative, Number.Singular) => ["ito", "iyā", "īto"],
            (NounCase.Ablative, Number.Plural) => ["ibhi", "ībhi", "īhi"],
            (NounCase.Genitive, Number.Singular) => ["iyā"],
            (NounCase.Genitive, Number.Plural) => ["īnaṃ"],
            (NounCase.Locative, Number.Singular) => ["iyaṃ", "iyā"],
            (NounCase.Locative, Number.Plural) => ["isu", "īsu"],
            (NounCase.Vocative, Number.Singular) => ["i", "ī"],
            (NounCase.Vocative, Number.Plural) => ["iyo", "ī"],
            _ => Array.Empty<string>()
        };
    }

    static string[] GetU_Masc(NounCase nounCase, Number number)
    {
        return (nounCase, number) switch
        {
            (NounCase.Nominative, Number.Singular) => ["u"],
            (NounCase.Nominative, Number.Plural) => ["avo", "ū"],
            (NounCase.Accusative, Number.Singular) => ["unaṃ", "uṃ"],
            (NounCase.Accusative, Number.Plural) => ["avo", "ū"],
            (NounCase.Instrumental, Number.Singular) => ["unā"],
            (NounCase.Instrumental, Number.Plural) => ["ūhi"],
            (NounCase.Dative, Number.Singular) => ["uno", "ussa"],
            (NounCase.Dative, Number.Plural) => ["unaṃ", "ūnaṃ"],
            (NounCase.Ablative, Number.Singular) => ["uto", "unā", "umhā", "usmā"],
            (NounCase.Ablative, Number.Plural) => ["ūhi"],
            (NounCase.Genitive, Number.Singular) => ["uno", "ussa"],
            (NounCase.Genitive, Number.Plural) => ["unaṃ", "ūnaṃ"],
            (NounCase.Locative, Number.Singular) => ["umhi", "usmiṃ"],
            (NounCase.Locative, Number.Plural) => ["usu", "ūsu"],
            (NounCase.Vocative, Number.Singular) => ["u"],
            (NounCase.Vocative, Number.Plural) => ["ave", "avo", "ū"],
            _ => Array.Empty<string>()
        };
    }

    static string[] GetU_Nt(NounCase nounCase, Number number)
    {
        return (nounCase, number) switch
        {
            (NounCase.Nominative, Number.Singular) => ["u", "uṃ"],
            (NounCase.Nominative, Number.Plural) => ["ū", "ūni"],
            (NounCase.Accusative, Number.Singular) => ["uṃ"],
            (NounCase.Accusative, Number.Plural) => ["ū", "ūni"],
            (NounCase.Instrumental, Number.Singular) => ["unā", "usā"],
            (NounCase.Instrumental, Number.Plural) => ["ūhi"],
            (NounCase.Dative, Number.Singular) => ["uno", "ussa"],
            (NounCase.Dative, Number.Plural) => ["ūnaṃ"],
            (NounCase.Ablative, Number.Singular) => ["uto", "unā", "umhā", "usmā"],
            (NounCase.Ablative, Number.Plural) => ["ūhi"],
            (NounCase.Genitive, Number.Singular) => ["uno", "ussa"],
            (NounCase.Genitive, Number.Plural) => ["ūnaṃ"],
            (NounCase.Locative, Number.Singular) => ["umhi", "usmiṃ"],
            (NounCase.Locative, Number.Plural) => ["usu", "ūsu"],
            (NounCase.Vocative, Number.Singular) => ["u"],
            (NounCase.Vocative, Number.Plural) => ["ū", "ūni"],
            _ => Array.Empty<string>()
        };
    }

    static string[] GetU_Fem(NounCase nounCase, Number number)
    {
        return (nounCase, number) switch
        {
            (NounCase.Nominative, Number.Singular) => ["u"],
            (NounCase.Nominative, Number.Plural) => ["uyo", "ū"],
            (NounCase.Accusative, Number.Singular) => ["uṃ"],
            (NounCase.Accusative, Number.Plural) => ["uyo", "ū"],
            (NounCase.Instrumental, Number.Singular) => ["uyā"],
            (NounCase.Instrumental, Number.Plural) => ["ūhi"],
            (NounCase.Dative, Number.Singular) => ["uyā"],
            (NounCase.Dative, Number.Plural) => ["ūnaṃ"],
            (NounCase.Ablative, Number.Singular) => ["uto", "uyā"],
            (NounCase.Ablative, Number.Plural) => ["ūhi"],
            (NounCase.Genitive, Number.Singular) => ["uyā"],
            (NounCase.Genitive, Number.Plural) => ["ūnaṃ"],
            (NounCase.Locative, Number.Singular) => ["uyaṃ", "uyā"],
            (NounCase.Locative, Number.Plural) => ["usu", "ūsu"],
            (NounCase.Vocative, Number.Singular) => ["u"],
            (NounCase.Vocative, Number.Plural) => ["uyo", "ū"],
            _ => Array.Empty<string>()
        };
    }

    static string[] GetAs_Masc(NounCase nounCase, Number number)
    {
        return (nounCase, number) switch
        {
            (NounCase.Nominative, Number.Singular) => ["o"],
            (NounCase.Nominative, Number.Plural) => ["ā", "āni"],
            (NounCase.Accusative, Number.Singular) => ["aṃ", "o"],
            (NounCase.Accusative, Number.Plural) => ["e"],
            (NounCase.Instrumental, Number.Singular) => ["asā", "ena"],
            (NounCase.Instrumental, Number.Plural) => ["ehi"],
            (NounCase.Dative, Number.Singular) => ["aso", "assa"],
            (NounCase.Dative, Number.Plural) => ["ānaṃ"],
            (NounCase.Ablative, Number.Singular) => ["ato", "amhā", "asā", "asmā", "ā"],
            (NounCase.Ablative, Number.Plural) => ["ehi"],
            (NounCase.Genitive, Number.Singular) => ["aso", "assa"],
            (NounCase.Genitive, Number.Plural) => ["ānaṃ"],
            (NounCase.Locative, Number.Singular) => ["amhi", "asi", "asmiṃ", "e"],
            (NounCase.Locative, Number.Plural) => ["esu"],
            (NounCase.Vocative, Number.Singular) => ["a", "ā"],
            (NounCase.Vocative, Number.Plural) => ["ā"],
            _ => Array.Empty<string>()
        };
    }

    static string[] GetAr_Masc(NounCase nounCase, Number number)
    {
        return (nounCase, number) switch
        {
            (NounCase.Nominative, Number.Singular) => ["ā"],
            (NounCase.Nominative, Number.Plural) => ["āro"],
            (NounCase.Accusative, Number.Singular) => ["araṃ", "āraṃ"],
            (NounCase.Accusative, Number.Plural) => ["āre", "āro"],
            (NounCase.Instrumental, Number.Singular) => ["ārā", "unā"],
            (NounCase.Instrumental, Number.Plural) => ["ārehi", "ubhi"],
            (NounCase.Dative, Number.Singular) => ["u", "uno", "ussa"],
            (NounCase.Dative, Number.Plural) => ["ānaṃ", "ārānaṃ"],
            (NounCase.Ablative, Number.Singular) => ["ārā"],
            (NounCase.Ablative, Number.Plural) => ["ārehi", "ubhi"],
            (NounCase.Genitive, Number.Singular) => ["u", "uno", "ussa"],
            (NounCase.Genitive, Number.Plural) => ["ānaṃ", "ārānaṃ"],
            (NounCase.Locative, Number.Singular) => ["ari"],
            (NounCase.Locative, Number.Plural) => ["āresu"],
            (NounCase.Vocative, Number.Singular) => ["a", "ā", "e"],
            (NounCase.Vocative, Number.Plural) => ["āro"],
            _ => Array.Empty<string>()
        };
    }
}
