using PaliPractice.Localization;

namespace PaliPractice.Presentation.Practice.Common;

/// <summary>
/// Static helper providing full and abbreviated badge labels for grammatical categories.
/// Abbreviations are used when badges don't fit in narrow card widths.
/// </summary>
public static class BadgeLabelMaps
{
    #region Gender

    public static string GetFull(Gender gender) => gender switch
    {
        Gender.Masculine => GrammarText.GetGender(Gender.Masculine),
        Gender.Feminine => GrammarText.GetGender(Gender.Feminine),
        Gender.Neuter => GrammarText.GetGender(Gender.Neuter),
        _ => gender.ToString()
    };

    public static string GetAbbreviated(Gender gender) => gender switch
    {
        Gender.Masculine => GrammarText.GetGenderShort(Gender.Masculine),
        Gender.Feminine => GrammarText.GetGenderShort(Gender.Feminine),
        Gender.Neuter => GrammarText.GetGenderShort(Gender.Neuter),
        _ => gender.ToString()
    };

    #endregion

    #region Number

    public static string GetFull(Number number) => number switch
    {
        Number.Singular => GrammarText.GetNumber(Number.Singular),
        Number.Plural => GrammarText.GetNumber(Number.Plural),
        _ => number.ToString()
    };

    public static string GetAbbreviated(Number number) => number switch
    {
        Number.Singular => GrammarText.GetNumberShort(Number.Singular),
        Number.Plural => GrammarText.GetNumberShort(Number.Plural),
        _ => number.ToString()
    };

    #endregion

    #region Voice

    public static string GetFull(Voice voice) => voice switch
    {
        Voice.Active => GrammarText.GetVoice(Voice.Active),
        Voice.Reflexive => GrammarText.GetVoice(Voice.Reflexive),
        _ => voice.ToString()
    };

    public static string GetAbbreviated(Voice voice) => voice switch
    {
        Voice.Active => GrammarText.GetVoiceShort(Voice.Active),
        Voice.Reflexive => GrammarText.GetVoiceShort(Voice.Reflexive),
        _ => voice.ToString()
    };

    #endregion
}
