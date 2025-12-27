namespace PaliPractice.Models.Inflection;

/// <summary>
/// Provides noun declension endings for patterns.
/// Regular patterns return endings; irregular patterns return full forms.
/// </summary>
public static partial class NounEndings
{
    /// <summary>
    /// Get declension endings for a noun pattern.
    /// </summary>
    /// <param name="pattern">The noun pattern enum value</param>
    /// <param name="nounCase">Grammatical case</param>
    /// <param name="number">Singular or plural</param>
    /// <returns>Array of endings. Empty array if pattern not supported.</returns>
    public static string[] GetEndings(NounPattern pattern, Case nounCase, Number number)
    {
        return pattern switch
        {
            // Masculine patterns
            NounPattern.AMasc => GetA_Masc(nounCase, number),
            NounPattern.IMasc => GetI_Masc(nounCase, number),
            NounPattern.ĪMasc => GetI_Long_Masc(nounCase, number),
            NounPattern.UMasc => GetU_Masc(nounCase, number),
            NounPattern.ŪMasc => GetU_Long_Masc(nounCase, number),
            NounPattern.AsMasc => GetAs_Masc(nounCase, number),
            NounPattern.ArMasc => GetAr_Masc(nounCase, number),
            NounPattern.AntMasc => GetAnt_Masc(nounCase, number),

            // Neuter patterns
            NounPattern.ANeut => GetA_Nt(nounCase, number),
            NounPattern.INeut => GetI_Nt(nounCase, number),
            NounPattern.UNeut => GetU_Nt(nounCase, number),

            // Feminine patterns
            NounPattern.ĀFem => GetA_Fem(nounCase, number),
            NounPattern.IFem => GetI_Fem(nounCase, number),
            NounPattern.ĪFem => GetI_Long_Fem(nounCase, number),
            NounPattern.UFem => GetU_Fem(nounCase, number),

            // Irregular patterns (defined in NounEndingsIrregular.cs)
            NounPattern.RājaMasc => GetRaja_Masc(nounCase, number),
            NounPattern.BrahmaMasc => GetBrahma_Masc(nounCase, number),
            NounPattern.KammaNeut => GetKamma_Nt(nounCase, number),

            _ => []
        };
    }

    #region Regular patterns

    /// <summary>a masc - like dhamma</summary>
    static string[] GetA_Masc(Case nounCase, Number number)
    {
        return (nounCase, number) switch
        {
            (Case.Nominative, Number.Singular) => ["o"],
            (Case.Nominative, Number.Plural) => ["ā", "āse"],
            (Case.Accusative, Number.Singular) => ["aṃ"],
            (Case.Accusative, Number.Plural) => ["e"],
            (Case.Instrumental, Number.Singular) => ["ā", "ena"],
            (Case.Instrumental, Number.Plural) => ["ebhi", "ehi"],
            (Case.Dative, Number.Singular) => ["assa", "āya"],
            (Case.Dative, Number.Plural) => ["ānaṃ"],
            (Case.Ablative, Number.Singular) => ["ato", "amhā", "asmā", "ā"],
            (Case.Ablative, Number.Plural) => ["ato", "ebhi", "ehi"],
            (Case.Genitive, Number.Singular) => ["assa"],
            (Case.Genitive, Number.Plural) => ["āna", "ānaṃ"],
            (Case.Locative, Number.Singular) => ["amhi", "asmiṃ", "e"],
            (Case.Locative, Number.Plural) => ["esu"],
            (Case.Vocative, Number.Singular) => ["a", "ā"],
            (Case.Vocative, Number.Plural) => ["ā"],
            _ => []
        };
    }

    static string[] GetA_Nt(Case nounCase, Number number)
    {
        return (nounCase, number) switch
        {
            (Case.Nominative, Number.Singular) => ["aṃ"],
            (Case.Nominative, Number.Plural) => ["ā", "āni"],
            (Case.Accusative, Number.Singular) => ["aṃ"],
            (Case.Accusative, Number.Plural) => ["āni", "e"],
            (Case.Instrumental, Number.Singular) => ["ā", "ena"],
            (Case.Instrumental, Number.Plural) => ["ebhi", "ehi"],
            (Case.Dative, Number.Singular) => ["assa", "āya"],
            (Case.Dative, Number.Plural) => ["ānaṃ"],
            (Case.Ablative, Number.Singular) => ["ato", "amhā", "asmā", "ā"],
            (Case.Ablative, Number.Plural) => ["ebhi", "ehi"],
            (Case.Genitive, Number.Singular) => ["assa"],
            (Case.Genitive, Number.Plural) => ["āna", "ānaṃ"],
            (Case.Locative, Number.Singular) => ["amhi", "asmiṃ", "e"],
            (Case.Locative, Number.Plural) => ["esu"],
            (Case.Vocative, Number.Singular) => ["a", "aṃ", "ā"],
            (Case.Vocative, Number.Plural) => ["ā", "āni"],
            _ => []
        };
    }

    static string[] GetA_Fem(Case nounCase, Number number)
    {
        return (nounCase, number) switch
        {
            (Case.Nominative, Number.Singular) => ["ā"],
            (Case.Nominative, Number.Plural) => ["ā", "āyo"],
            (Case.Accusative, Number.Singular) => ["aṃ"],
            (Case.Accusative, Number.Plural) => ["ā", "āyo"],
            (Case.Instrumental, Number.Singular) => ["ā", "āya"],
            (Case.Instrumental, Number.Plural) => ["ābhi", "āhi"],
            (Case.Dative, Number.Singular) => ["āya"],
            (Case.Dative, Number.Plural) => ["ānaṃ"],
            (Case.Ablative, Number.Singular) => ["ato", "āto", "āya"],
            (Case.Ablative, Number.Plural) => ["ābhi", "āhi"],
            (Case.Genitive, Number.Singular) => ["āya"],
            (Case.Genitive, Number.Plural) => ["ānaṃ"],
            (Case.Locative, Number.Singular) => ["āya", "āyaṃ"],
            (Case.Locative, Number.Plural) => ["āsu"],
            (Case.Vocative, Number.Singular) => ["a", "e"],
            (Case.Vocative, Number.Plural) => ["ā", "āyo"],
            _ => []
        };
    }

    static string[] GetI_Masc(Case nounCase, Number number)
    {
        return (nounCase, number) switch
        {
            (Case.Nominative, Number.Singular) => ["i"],
            (Case.Nominative, Number.Plural) => ["ayo", "ī"],
            (Case.Accusative, Number.Singular) => ["iṃ"],
            (Case.Accusative, Number.Plural) => ["ayo", "ī"],
            (Case.Instrumental, Number.Singular) => ["inā"],
            (Case.Instrumental, Number.Plural) => ["ibhi", "ībhi", "īhi"],
            (Case.Dative, Number.Singular) => ["ino", "issa"],
            (Case.Dative, Number.Plural) => ["inaṃ", "īnaṃ"],
            (Case.Ablative, Number.Singular) => ["ito", "inā", "imhā", "ismā"],
            (Case.Ablative, Number.Plural) => ["ibhi", "ībhi", "īhi"],
            (Case.Genitive, Number.Singular) => ["ino", "issa"],
            (Case.Genitive, Number.Plural) => ["inaṃ", "īnaṃ"],
            (Case.Locative, Number.Singular) => ["imhi", "ismiṃ"],
            (Case.Locative, Number.Plural) => ["isu", "īsu"],
            (Case.Vocative, Number.Singular) => ["i", "e"],
            (Case.Vocative, Number.Plural) => ["ayo", "ī"],
            _ => []
        };
    }

    static string[] GetI_Fem(Case nounCase, Number number)
    {
        return (nounCase, number) switch
        {
            (Case.Nominative, Number.Singular) => ["i"],
            (Case.Nominative, Number.Plural) => ["iyo", "ī"],
            (Case.Accusative, Number.Singular) => ["iṃ"],
            (Case.Accusative, Number.Plural) => ["iyo", "ī"],
            (Case.Instrumental, Number.Singular) => ["iyā"],
            (Case.Instrumental, Number.Plural) => ["ibhi", "ībhi", "īhi"],
            (Case.Dative, Number.Singular) => ["iyā"],
            (Case.Dative, Number.Plural) => ["īnaṃ"],
            (Case.Ablative, Number.Singular) => ["ito", "iyā"],
            (Case.Ablative, Number.Plural) => ["ibhi", "ībhi", "īhi"],
            (Case.Genitive, Number.Singular) => ["iyā"],
            (Case.Genitive, Number.Plural) => ["īnaṃ"],
            (Case.Locative, Number.Singular) => ["iyaṃ", "iyā"],
            (Case.Locative, Number.Plural) => ["isu", "īsu"],
            (Case.Vocative, Number.Singular) => ["i"],
            (Case.Vocative, Number.Plural) => ["iyo", "ī"],
            _ => []
        };
    }

    static string[] GetI_Long_Masc(Case nounCase, Number number)
    {
        return (nounCase, number) switch
        {
            (Case.Nominative, Number.Singular) => ["ī"],
            (Case.Nominative, Number.Plural) => ["ino", "ī"],
            (Case.Accusative, Number.Singular) => ["inaṃ", "iṃ"],
            (Case.Accusative, Number.Plural) => ["ino", "ī"],
            (Case.Instrumental, Number.Singular) => ["inā"],
            (Case.Instrumental, Number.Plural) => ["ibhi", "īhi"],
            (Case.Dative, Number.Singular) => ["ino", "issa"],
            (Case.Dative, Number.Plural) => ["inaṃ", "īnaṃ"],
            (Case.Ablative, Number.Singular) => ["inā", "imhā"],
            (Case.Ablative, Number.Plural) => ["ibhi", "īhi"],
            (Case.Genitive, Number.Singular) => ["ino", "issa"],
            (Case.Genitive, Number.Plural) => ["inaṃ", "īnaṃ"],
            (Case.Locative, Number.Singular) => ["ini", "imhi", "ismiṃ"],
            (Case.Locative, Number.Plural) => ["isu", "īsu"],
            (Case.Vocative, Number.Singular) => ["i", "ī"],
            (Case.Vocative, Number.Plural) => ["ino", "ī"],
            _ => []
        };
    }

    static string[] GetI_Long_Fem(Case nounCase, Number number)
    {
        return (nounCase, number) switch
        {
            (Case.Nominative, Number.Singular) => ["ī"],
            (Case.Nominative, Number.Plural) => ["iyo", "ī"],
            (Case.Accusative, Number.Singular) => ["iṃ"],
            (Case.Accusative, Number.Plural) => ["iyo", "ī"],
            (Case.Instrumental, Number.Singular) => ["iyā"],
            (Case.Instrumental, Number.Plural) => ["ibhi", "ībhi", "īhi"],
            (Case.Dative, Number.Singular) => ["iyā"],
            (Case.Dative, Number.Plural) => ["īnaṃ"],
            (Case.Ablative, Number.Singular) => ["ito", "iyā", "īto"],
            (Case.Ablative, Number.Plural) => ["ibhi", "ībhi", "īhi"],
            (Case.Genitive, Number.Singular) => ["iyā"],
            (Case.Genitive, Number.Plural) => ["īnaṃ"],
            (Case.Locative, Number.Singular) => ["iyaṃ", "iyā"],
            (Case.Locative, Number.Plural) => ["isu", "īsu"],
            (Case.Vocative, Number.Singular) => ["i", "ī"],
            (Case.Vocative, Number.Plural) => ["iyo", "ī"],
            _ => []
        };
    }

    static string[] GetU_Masc(Case nounCase, Number number)
    {
        return (nounCase, number) switch
        {
            (Case.Nominative, Number.Singular) => ["u"],
            (Case.Nominative, Number.Plural) => ["avo", "ū"],
            (Case.Accusative, Number.Singular) => ["unaṃ", "uṃ"],
            (Case.Accusative, Number.Plural) => ["avo", "ū"],
            (Case.Instrumental, Number.Singular) => ["unā"],
            (Case.Instrumental, Number.Plural) => ["ūhi"],
            (Case.Dative, Number.Singular) => ["uno", "ussa"],
            (Case.Dative, Number.Plural) => ["unaṃ", "ūnaṃ"],
            (Case.Ablative, Number.Singular) => ["uto", "unā", "umhā", "usmā"],
            (Case.Ablative, Number.Plural) => ["ūhi"],
            (Case.Genitive, Number.Singular) => ["uno", "ussa"],
            (Case.Genitive, Number.Plural) => ["unaṃ", "ūnaṃ"],
            (Case.Locative, Number.Singular) => ["umhi", "usmiṃ"],
            (Case.Locative, Number.Plural) => ["usu", "ūsu"],
            (Case.Vocative, Number.Singular) => ["u"],
            (Case.Vocative, Number.Plural) => ["ave", "avo", "ū"],
            _ => []
        };
    }

    static string[] GetU_Nt(Case nounCase, Number number)
    {
        return (nounCase, number) switch
        {
            (Case.Nominative, Number.Singular) => ["u", "uṃ"],
            (Case.Nominative, Number.Plural) => ["ū", "ūni"],
            (Case.Accusative, Number.Singular) => ["uṃ"],
            (Case.Accusative, Number.Plural) => ["ū", "ūni"],
            (Case.Instrumental, Number.Singular) => ["unā", "usā"],
            (Case.Instrumental, Number.Plural) => ["ūhi"],
            (Case.Dative, Number.Singular) => ["uno", "ussa"],
            (Case.Dative, Number.Plural) => ["ūnaṃ"],
            (Case.Ablative, Number.Singular) => ["uto", "unā", "umhā", "usmā"],
            (Case.Ablative, Number.Plural) => ["ūhi"],
            (Case.Genitive, Number.Singular) => ["uno", "ussa"],
            (Case.Genitive, Number.Plural) => ["ūnaṃ"],
            (Case.Locative, Number.Singular) => ["umhi", "usmiṃ"],
            (Case.Locative, Number.Plural) => ["usu", "ūsu"],
            (Case.Vocative, Number.Singular) => ["u"],
            (Case.Vocative, Number.Plural) => ["ū", "ūni"],
            _ => []
        };
    }

    static string[] GetU_Fem(Case nounCase, Number number)
    {
        return (nounCase, number) switch
        {
            (Case.Nominative, Number.Singular) => ["u"],
            (Case.Nominative, Number.Plural) => ["uyo", "ū"],
            (Case.Accusative, Number.Singular) => ["uṃ"],
            (Case.Accusative, Number.Plural) => ["uyo", "ū"],
            (Case.Instrumental, Number.Singular) => ["uyā"],
            (Case.Instrumental, Number.Plural) => ["ūhi"],
            (Case.Dative, Number.Singular) => ["uyā"],
            (Case.Dative, Number.Plural) => ["ūnaṃ"],
            (Case.Ablative, Number.Singular) => ["uto", "uyā"],
            (Case.Ablative, Number.Plural) => ["ūhi"],
            (Case.Genitive, Number.Singular) => ["uyā"],
            (Case.Genitive, Number.Plural) => ["ūnaṃ"],
            (Case.Locative, Number.Singular) => ["uyaṃ", "uyā"],
            (Case.Locative, Number.Plural) => ["usu", "ūsu"],
            (Case.Vocative, Number.Singular) => ["u"],
            (Case.Vocative, Number.Plural) => ["uyo", "ū"],
            _ => []
        };
    }

    static string[] GetAs_Masc(Case nounCase, Number number)
    {
        return (nounCase, number) switch
        {
            (Case.Nominative, Number.Singular) => ["o"],
            (Case.Nominative, Number.Plural) => ["ā", "āni"],
            (Case.Accusative, Number.Singular) => ["aṃ", "o"],
            (Case.Accusative, Number.Plural) => ["e"],
            (Case.Instrumental, Number.Singular) => ["asā", "ena"],
            (Case.Instrumental, Number.Plural) => ["ehi"],
            (Case.Dative, Number.Singular) => ["aso", "assa"],
            (Case.Dative, Number.Plural) => ["ānaṃ"],
            (Case.Ablative, Number.Singular) => ["ato", "amhā", "asā", "asmā", "ā"],
            (Case.Ablative, Number.Plural) => ["ehi"],
            (Case.Genitive, Number.Singular) => ["aso", "assa"],
            (Case.Genitive, Number.Plural) => ["ānaṃ"],
            (Case.Locative, Number.Singular) => ["amhi", "asi", "asmiṃ", "e"],
            (Case.Locative, Number.Plural) => ["esu"],
            (Case.Vocative, Number.Singular) => ["a", "ā"],
            (Case.Vocative, Number.Plural) => ["ā"],
            _ => []
        };
    }

    /// <summary>ar masc - like satthar</summary>
    static string[] GetAr_Masc(Case nounCase, Number number)
    {
        return (nounCase, number) switch
        {
            (Case.Nominative, Number.Singular) => ["ā"],
            (Case.Nominative, Number.Plural) => ["āro"],
            (Case.Accusative, Number.Singular) => ["araṃ", "āraṃ"],
            (Case.Accusative, Number.Plural) => ["āre", "āro"],
            (Case.Instrumental, Number.Singular) => ["ārā", "unā"],
            (Case.Instrumental, Number.Plural) => ["ārehi", "ubhi"],
            (Case.Dative, Number.Singular) => ["u", "uno", "ussa"],
            (Case.Dative, Number.Plural) => ["ānaṃ", "ārānaṃ", "ūnaṃ"],
            (Case.Ablative, Number.Singular) => ["ārā"],
            (Case.Ablative, Number.Plural) => ["ārehi", "ubhi"],
            (Case.Genitive, Number.Singular) => ["u", "uno", "ussa"],
            (Case.Genitive, Number.Plural) => ["ānaṃ", "ārānaṃ", "ūnaṃ"],
            (Case.Locative, Number.Singular) => ["ari"],
            (Case.Locative, Number.Plural) => ["āresu"],
            (Case.Vocative, Number.Singular) => ["a", "ā", "e"],
            (Case.Vocative, Number.Plural) => ["āro"],
            _ => []
        };
    }

    /// <summary>i nt - like sappi</summary>
    static string[] GetI_Nt(Case nounCase, Number number)
    {
        return (nounCase, number) switch
        {
            (Case.Nominative, Number.Singular) => ["i", "iṃ"],
            (Case.Nominative, Number.Plural) => ["ī", "īni"],
            (Case.Accusative, Number.Singular) => ["iṃ"],
            (Case.Accusative, Number.Plural) => ["ī", "īni"],
            (Case.Instrumental, Number.Singular) => ["inā"],
            (Case.Instrumental, Number.Plural) => ["ibhi", "īhi"],
            (Case.Dative, Number.Singular) => ["ino", "issa"],
            (Case.Dative, Number.Plural) => ["īnaṃ"],
            (Case.Ablative, Number.Singular) => ["ito", "inā", "imhā", "ismā"],
            (Case.Ablative, Number.Plural) => ["ibhi", "īhi"],
            (Case.Genitive, Number.Singular) => ["ino", "issa"],
            (Case.Genitive, Number.Plural) => ["īnaṃ"],
            (Case.Locative, Number.Singular) => ["ini", "imhi", "ismiṃ"],
            (Case.Locative, Number.Plural) => ["isu", "īsu"],
            (Case.Vocative, Number.Singular) => ["i"],
            (Case.Vocative, Number.Plural) => ["ī", "īni"],
            _ => []
        };
    }

    /// <summary>ū masc - like lokavidū</summary>
    static string[] GetU_Long_Masc(Case nounCase, Number number)
    {
        return (nounCase, number) switch
        {
            (Case.Nominative, Number.Singular) => ["ū"],
            (Case.Nominative, Number.Plural) => ["uno", "ū", "ūno"],
            (Case.Accusative, Number.Singular) => ["unaṃ", "uṃ"],
            (Case.Accusative, Number.Plural) => ["uno", "ū", "ūno"],
            (Case.Instrumental, Number.Singular) => ["unā"],
            (Case.Instrumental, Number.Plural) => ["ūhi"],
            (Case.Dative, Number.Singular) => ["uno"],
            (Case.Dative, Number.Plural) => ["ūna", "ūnaṃ"],
            (Case.Ablative, Number.Singular) => ["unā", "ūto"],
            (Case.Ablative, Number.Plural) => ["ūhi"],
            (Case.Genitive, Number.Singular) => ["uno", "ussa"],
            (Case.Genitive, Number.Plural) => ["unnaṃ", "ūna", "ūnaṃ"],
            (Case.Locative, Number.Singular) => ["umhi", "usmiṃ"],
            (Case.Locative, Number.Plural) => ["ūsu"],
            (Case.Vocative, Number.Singular) => ["ū"],
            (Case.Vocative, Number.Plural) => ["uno", "ū"],
            _ => []
        };
    }

    /// <summary>ant masc - like bhagavant</summary>
    static string[] GetAnt_Masc(Case nounCase, Number number)
    {
        return (nounCase, number) switch
        {
            (Case.Nominative, Number.Singular) => ["anto", "ā"],
            (Case.Nominative, Number.Plural) => ["antā", "anto", "ā"],
            (Case.Accusative, Number.Singular) => ["antaṃ"],
            (Case.Accusative, Number.Plural) => ["ante"],
            (Case.Instrumental, Number.Singular) => ["atā", "antena"],
            (Case.Instrumental, Number.Plural) => ["antehi"],
            (Case.Dative, Number.Singular) => ["ato", "antassa"],
            (Case.Dative, Number.Plural) => ["ataṃ", "antānaṃ"],
            (Case.Ablative, Number.Singular) => ["atā", "ato", "antamhā", "antasmā", "antā"],
            (Case.Ablative, Number.Plural) => ["antehi"],
            (Case.Genitive, Number.Singular) => ["ato", "antassa", "assa"],
            (Case.Genitive, Number.Plural) => ["ataṃ", "antānaṃ"],
            (Case.Locative, Number.Singular) => ["ati", "antamhi", "antasmiṃ", "ante"],
            (Case.Locative, Number.Plural) => ["antesu"],
            (Case.Vocative, Number.Singular) => ["a", "aṃ", "ā"],
            (Case.Vocative, Number.Plural) => ["antā", "anto"],
            _ => []
        };
    }

    #endregion
}
