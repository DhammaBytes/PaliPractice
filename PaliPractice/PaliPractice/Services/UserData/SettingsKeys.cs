using PaliPractice.Models;

namespace PaliPractice.Services.UserData;

/// <summary>
/// Constants for user settings keys stored in user_data.db.
/// All multi-value settings are stored as CSV of enum integers (e.g., "1,2,3").
/// </summary>
public static class SettingsKeys
{
    // ═══════════════════════════════════════════
    // NOUNS (Declension) Settings
    // ═══════════════════════════════════════════

    public const string NounsDailyGoal = "nouns.daily_goal";
    public const string NounsCases = "nouns.cases";
    public const string NounsNumbers = "nouns.numbers";
    public const string NounsLemmaMin = "nouns.lemma_min";
    public const string NounsLemmaMax = "nouns.lemma_max";

    // Enabled patterns per gender (CSV of NounPattern enum values)
    public const string NounsMascPatterns = "nouns.masc_patterns";
    public const string NounsNeutPatterns = "nouns.neut_patterns";
    public const string NounsFemPatterns = "nouns.fem_patterns";

    // ═══════════════════════════════════════════
    // VERBS (Conjugation) Settings
    // ═══════════════════════════════════════════

    public const string VerbsDailyGoal = "verbs.daily_goal";
    public const string VerbsTenses = "verbs.tenses";
    public const string VerbsPersons = "verbs.persons";
    public const string VerbsNumbers = "verbs.numbers";
    public const string VerbsVoices = "verbs.voices";  // Voice enum: Normal=1, Reflexive=2
    public const string VerbsLemmaMin = "verbs.lemma_min";
    public const string VerbsLemmaMax = "verbs.lemma_max";

    // Enabled patterns (CSV of VerbPattern enum values)
    public const string VerbsPatterns = "verbs.patterns";

    // ═══════════════════════════════════════════
    // Default Values (as typed arrays)
    // Use SettingsHelpers.ToCsv() to convert to strings for storage.
    // ═══════════════════════════════════════════

    public const int DefaultDailyGoal = 50;
    public const int DefaultLemmaMin = 1;
    public const int DefaultLemmaMax = 100;

    // Nouns: All cases except Vocative
    public static readonly Case[] NounsDefaultCases =
    [
        Case.Nominative, Case.Accusative, Case.Instrumental,
        Case.Dative, Case.Ablative, Case.Genitive, Case.Locative
    ];

    // Both singular and plural
    public static readonly Number[] DefaultNumbers = [Number.Singular, Number.Plural];

    // Nouns: All regular masculine patterns (1-8)
    public static readonly NounPattern[] NounsDefaultMascPatterns =
    [
        NounPattern.AMasc, NounPattern.IMasc, NounPattern.ĪMasc, NounPattern.UMasc,
        NounPattern.ŪMasc, NounPattern.ArMasc, NounPattern.AntMasc, NounPattern.AsMasc
    ];

    // Nouns: All regular feminine patterns
    public static readonly NounPattern[] NounsDefaultFemPatterns =
    [
        NounPattern.ĀFem, NounPattern.IFem, NounPattern.ĪFem,
        NounPattern.UFem, NounPattern.ArFem
    ];

    // Nouns: All regular neuter patterns
    public static readonly NounPattern[] NounsDefaultNeutPatterns =
    [
        NounPattern.ANeut, NounPattern.INeut, NounPattern.UNeut
    ];

    // Verbs: All tenses
    public static readonly Tense[] VerbsDefaultTenses =
    [
        Tense.Present, Tense.Imperative, Tense.Optative, Tense.Future
    ];

    // Verbs: All persons
    public static readonly Person[] VerbsDefaultPersons =
    [
        Person.First, Person.Second, Person.Third
    ];

    // Verbs: Both normal (active) and reflexive voices
    public static readonly Voice[] VerbsDefaultVoices = [Voice.Normal, Voice.Reflexive];

    // Verbs: All regular patterns (1-4)
    public static readonly VerbPattern[] VerbsDefaultPatterns =
    [
        VerbPattern.Ati, VerbPattern.Āti, VerbPattern.Eti, VerbPattern.Oti
    ];
}
