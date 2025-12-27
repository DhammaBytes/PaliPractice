namespace PaliPractice.Services.UserData;

/// <summary>
/// Constants for user settings keys stored in user_data.db.
/// </summary>
public static class SettingsKeys
{
    // Daily goals
    public const string DeclensionDailyGoal = "declension.daily_goal";
    public const string ConjugationDailyGoal = "conjugation.daily_goal";

    // Declension filters (comma-separated enum values, e.g., "1,2,3")
    public const string DeclensionCases = "declension.cases";
    public const string DeclensionGenders = "declension.genders";
    public const string DeclensionNumbers = "declension.numbers";
    public const string DeclensionLemmaMin = "declension.lemma_min";  // Min rank by EbtCount
    public const string DeclensionLemmaMax = "declension.lemma_max";  // Max rank by EbtCount

    // Declension pattern exclusions (comma-separated pattern stems, e.g., "ar,ant")
    public const string DeclensionMascExcludedPatterns = "declension.masc_excluded";
    public const string DeclensionNtExcludedPatterns = "declension.nt_excluded";
    public const string DeclensionFemExcludedPatterns = "declension.fem_excluded";

    // Declension number setting: "Both", "Singular", "Plural"
    public const string DeclensionNumberSetting = "declension.number_setting";

    // Conjugation filters
    public const string ConjugationTenses = "conjugation.tenses";
    public const string ConjugationPersons = "conjugation.persons";
    public const string ConjugationNumbers = "conjugation.numbers";
    public const string ConjugationReflexive = "conjugation.reflexive";  // "both", "active", "reflexive"
    public const string ConjugationLemmaMin = "conjugation.lemma_min";
    public const string ConjugationLemmaMax = "conjugation.lemma_max";

    // Conjugation pattern exclusions (comma-separated pattern stems, e.g., "eti,oti")
    // ati includes: ati pr, hoti pr, atthi pr
    // oti includes: oti pr, karoti pr, brūti pr
    public const string ConjugationExcludedPatterns = "conjugation.excluded_patterns";

    // Conjugation number setting: "Both", "Singular", "Plural"
    public const string ConjugationNumberSetting = "conjugation.number_setting";

    // Lemma range presets (0=Top100, 1=Top300, 2=Top500, 3=Custom)
    public const string DeclensionLemmaPreset = "declension.lemma_preset";
    public const string ConjugationLemmaPreset = "conjugation.lemma_preset";

    // Defaults
    public const int DefaultDailyGoal = 50;
    public const int DefaultLemmaMin = 1;
    public const int DefaultLemmaMax = 100;
    public const int DefaultLemmaPreset = 0;  // Top 100

    /// <summary>
    /// Default declension cases: all except Vocative (1-7).
    /// </summary>
    public const string DefaultDeclensionCases = "1,2,3,4,5,6,7";

    /// <summary>
    /// Default genders: all (1-3).
    /// </summary>
    public const string DefaultDeclensionGenders = "1,2,3";

    /// <summary>
    /// Default numbers: both singular and plural.
    /// </summary>
    public const string DefaultNumbers = "1,2";

    /// <summary>
    /// Default tenses: all four (Present, Imperative, Optative, Future).
    /// </summary>
    public const string DefaultConjugationTenses = "1,2,3,4";

    /// <summary>
    /// Default persons: all three.
    /// </summary>
    public const string DefaultConjugationPersons = "1,2,3";

    /// <summary>
    /// Default reflexive setting: include both active and reflexive forms.
    /// </summary>
    public const string DefaultReflexive = "both";
}
