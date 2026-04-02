using PaliPractice.Localization;

namespace PaliPractice.Services.UserData.Statistics;

/// <summary>
/// Helper for generating human-readable display names from grammatical combo keys.
/// </summary>
public static class ComboDisplayHelper
{
    /// <summary>
    /// Gets display name for a noun combo (case + gender + number).
    /// Example: "Nominative Masculine Singular"
    /// </summary>
    public static string GetNounComboDisplayName(Case @case, Gender gender, Number number)
    {
        return $"{GrammarText.GetCase(@case)} {GrammarText.GetGender(gender)} {GrammarText.GetNumber(number)}";
    }

    /// <summary>
    /// Gets display name for a verb combo (tense + person + number + voice).
    /// Example: "Present 3rd Singular" or "Present 3rd Singular Reflexive"
    /// </summary>
    public static string GetVerbComboDisplayName(Tense tense, Person person, Number number, Voice voice)
    {
        var personStr = GrammarText.GetPerson(person);

        var baseName = $"{GrammarText.GetTense(tense)} {personStr} {GrammarText.GetNumber(number)}";
        return voice == Voice.Reflexive ? $"{baseName} {GrammarText.GetVoice(Voice.Reflexive)}" : baseName;
    }

    /// <summary>
    /// Gets abbreviated display name for a noun combo.
    /// Example: "Nom Masc Sg"
    /// </summary>
    public static string GetNounComboShortName(Case @case, Gender gender, Number number)
    {
        var caseAbbr = GrammarText.GetCaseShort(@case);
        var genderAbbr = GrammarText.GetGenderShort(gender);
        var numberAbbr = GrammarText.GetNumberShort(number);

        return $"{caseAbbr} {genderAbbr} {numberAbbr}";
    }

    /// <summary>
    /// Gets abbreviated display name for a verb combo.
    /// Example: "Pr 3rd Sg" or "Pr 3rd Sg Reflx"
    /// </summary>
    public static string GetVerbComboShortName(Tense tense, Person person, Number number, Voice voice)
    {
        var tenseAbbr = GrammarText.GetTenseShort(tense);
        var personAbbr = GrammarText.GetPersonShort(person);
        var numberAbbr = GrammarText.GetNumberShort(number);

        var baseName = $"{tenseAbbr} {personAbbr} {numberAbbr}";
        return voice == Voice.Reflexive ? $"{baseName} {GrammarText.GetVoiceShort(Voice.Reflexive)}" : baseName;
    }
}
