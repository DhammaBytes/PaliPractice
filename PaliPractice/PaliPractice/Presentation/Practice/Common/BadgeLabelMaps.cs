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
        Gender.Masculine => "Masculine",
        Gender.Feminine => "Feminine",
        Gender.Neuter => "Neuter",
        _ => gender.ToString()
    };

    public static string GetAbbreviated(Gender gender) => gender switch
    {
        Gender.Masculine => "Masc.",
        Gender.Feminine => "Fem.",
        Gender.Neuter => "Neut.",
        _ => gender.ToString()
    };

    #endregion

    #region Number

    public static string GetFull(Number number) => number switch
    {
        Number.Singular => "Singular",
        Number.Plural => "Plural",
        _ => number.ToString()
    };

    public static string GetAbbreviated(Number number) => number switch
    {
        Number.Singular => "Sing.",
        Number.Plural => "Plur.",
        _ => number.ToString()
    };

    #endregion

    #region Voice

    public static string GetFull(Voice voice) => voice switch
    {
        Voice.Active => "Active",
        Voice.Reflexive => "Reflexive",
        _ => voice.ToString()
    };

    public static string GetAbbreviated(Voice voice) => voice switch
    {
        Voice.Active => "Active",
        Voice.Reflexive => "Refl.",
        _ => voice.ToString()
    };

    #endregion
}
