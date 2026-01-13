namespace PaliPractice.Themes;

/// <summary>
/// Helper for mapping grammar enums to SVG badge icon paths.
/// Icons are stored in Assets/Svg/Badges/ with consistent 12x12 viewBox.
/// </summary>
public static class BadgeIcons
{
    const string BasePath = "ms-appx:///Assets/Svg/Badges/";

    // Case icons
    public static string CaseNominative => $"{BasePath}case_nominative.svg";
    public static string CaseAccusative => $"{BasePath}case_accusative.svg";
    public static string CaseInstrumental => $"{BasePath}case_instrumental.svg";
    public static string CaseDative => $"{BasePath}case_dative.svg";
    public static string CaseAblative => $"{BasePath}case_ablative.svg";
    public static string CaseGenitive => $"{BasePath}case_genitive.svg";
    public static string CaseLocative => $"{BasePath}case_locative.svg";
    public static string CaseVocative => $"{BasePath}case_vocative.svg";

    // Gender icons
    public static string GenderMasculine => $"{BasePath}gender_male.svg";
    public static string GenderFeminine => $"{BasePath}gender_female.svg";
    public static string GenderNeuter => $"{BasePath}gender_neutral.svg";

    // Number icons (shared by nouns and verbs)
    public static string NumberSingular => $"{BasePath}number_singular.svg";
    public static string NumberPlural => $"{BasePath}number_plural.svg";

    // Person icons (verbs)
    public static string Person1st => $"{BasePath}person_1st.svg";
    public static string Person2nd => $"{BasePath}person_2nd.svg";
    public static string Person3rd => $"{BasePath}person_3rd.svg";

    // Tense icons (verbs)
    public static string TensePresent => $"{BasePath}tense_present.svg";
    public static string TenseImperative => $"{BasePath}tense_imperative.svg";
    public static string TenseOptative => $"{BasePath}tense_optative.svg";
    public static string TenseFuture => $"{BasePath}tense_future.svg";

    // Voice icons (verbs)
    public static string VoiceReflexive => $"{BasePath}voice_reflexive.svg";

    /// <summary>
    /// Gets the SVG icon path for any grammar enum value.
    /// Returns null if no icon is defined for the value.
    /// </summary>
    public static string? GetIconPath<T>(T value) where T : struct, Enum => value switch
    {
        // Cases
        Case.Nominative => CaseNominative,
        Case.Accusative => CaseAccusative,
        Case.Instrumental => CaseInstrumental,
        Case.Dative => CaseDative,
        Case.Ablative => CaseAblative,
        Case.Genitive => CaseGenitive,
        Case.Locative => CaseLocative,
        Case.Vocative => CaseVocative,

        // Genders
        Gender.Masculine => GenderMasculine,
        Gender.Feminine => GenderFeminine,
        Gender.Neuter => GenderNeuter,

        // Numbers
        Number.Singular => NumberSingular,
        Number.Plural => NumberPlural,

        // Persons
        Person.First => Person1st,
        Person.Second => Person2nd,
        Person.Third => Person3rd,

        // Tenses
        Tense.Present => TensePresent,
        Tense.Imperative => TenseImperative,
        Tense.Optative => TenseOptative,
        Tense.Future => TenseFuture,

        // Voice (only reflexive has an icon - active is the default)
        Voice.Reflexive => VoiceReflexive,

        _ => null
    };

    /// <summary>
    /// Gets the SVG icon path for reflexive voice indicator.
    /// Returns null for active voice (no icon needed).
    /// </summary>
    public static string? GetReflexiveIconPath(bool reflexive) =>
        reflexive ? VoiceReflexive : null;
}
