namespace PaliPractice.Themes;

/// <summary>
/// Helper for mapping grammar enums to badge icon paths.
/// Icons are stored as SVGs in Assets/Svg/Badges/ and rasterized to PNG via Uno.Resizetizer.
/// PNGs are used with BitmapIcon.ShowAsMonochrome for runtime tinting via Foreground brush.
///
/// ## Adding new SVG-to-PNG tintable icons
///
/// 1. Create SVG with solid shapes (color doesn't matter - only alpha channel is used for tinting)
/// 2. Place SVG in Assets/Svg/Badges/ (or similar folder under Assets/Svg/)
/// 3. Add UnoImage entry in .csproj:
///    <code>
///    &lt;UnoImage Include="Assets\Svg\Badges\my_icon.svg" BaseSize="24,24" /&gt;
///    </code>
/// 4. Reference as .png in code (Resizetizer generates PNGs at multiple scales):
///    <code>
///    public static string MyIcon =&gt; $"{BasePath}my_icon.png";
///    </code>
/// 5. Use with BitmapIcon for Foreground tinting:
///    <code>
///    new BitmapIcon()
///        .ShowAsMonochrome(true)
///        .Foreground(brush)
///        .UriSource(new Uri(BadgeIcons.MyIcon))
///    </code>
///
/// Notes:
/// - BaseSize determines the base raster size; Resizetizer generates scale variants (100-400%)
/// - SVG source color is ignored; ShowAsMonochrome uses alpha as mask and applies Foreground
/// - Only set Height on BitmapIcon (not Width) to preserve aspect ratio
/// </summary>
public static class BadgeIcons
{
    const string BasePath = "ms-appx:///Assets/Svg/Badges/";

    // Case icons
    public static string CaseNominative => $"{BasePath}case_nominative.png";
    public static string CaseAccusative => $"{BasePath}case_accusative.png";
    public static string CaseInstrumental => $"{BasePath}case_instrumental.png";
    public static string CaseDative => $"{BasePath}case_dative.png";
    public static string CaseAblative => $"{BasePath}case_ablative.png";
    public static string CaseGenitive => $"{BasePath}case_genitive.png";
    public static string CaseLocative => $"{BasePath}case_locative.png";
    public static string CaseVocative => $"{BasePath}case_vocative.png";

    // Gender icons
    public static string GenderMasculine => $"{BasePath}gender_male.png";
    public static string GenderFeminine => $"{BasePath}gender_female.png";
    public static string GenderNeuter => $"{BasePath}gender_neutral.png";

    // Number icons (shared by nouns and verbs)
    public static string NumberSingular => $"{BasePath}number_singular.png";
    public static string NumberPlural => $"{BasePath}number_plural.png";

    // Person icons (verbs)
    public static string Person1st => $"{BasePath}person_1st.png";
    public static string Person2nd => $"{BasePath}person_2nd.png";
    public static string Person3rd => $"{BasePath}person_3rd.png";

    // Tense icons (verbs)
    public static string TensePresent => $"{BasePath}tense_present.png";
    public static string TenseImperative => $"{BasePath}tense_imperative.png";
    public static string TenseOptative => $"{BasePath}tense_optative.png";
    public static string TenseFuture => $"{BasePath}tense_future.png";

    // Voice icons (verbs)
    public static string VoiceReflexive => $"{BasePath}voice_reflexive.png";

    /// <summary>
    /// Gets the PNG icon path for any grammar enum value.
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
    /// Gets the PNG icon path for reflexive voice indicator.
    /// Returns null for active voice (no icon needed).
    /// </summary>
    public static string? GetReflexiveIconPath(bool reflexive) =>
        reflexive ? VoiceReflexive : null;
}
