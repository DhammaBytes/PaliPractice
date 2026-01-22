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
        return $"{@case} {gender} {number}";
    }

    /// <summary>
    /// Gets display name for a verb combo (tense + person + number + voice).
    /// Example: "Present 3rd Singular" or "Present 3rd Singular Reflexive"
    /// </summary>
    public static string GetVerbComboDisplayName(Tense tense, Person person, Number number, Voice voice)
    {
        var personStr = person switch
        {
            Person.First => "1st",
            Person.Second => "2nd",
            Person.Third => "3rd",
            _ => person.ToString()
        };

        var baseName = $"{tense} {personStr} {number}";
        return voice == Voice.Reflexive ? $"{baseName} Reflexive" : baseName;
    }

    /// <summary>
    /// Gets abbreviated display name for a noun combo.
    /// Example: "Nom Masc Sg"
    /// </summary>
    public static string GetNounComboShortName(Case @case, Gender gender, Number number)
    {
        var caseAbbr = @case switch
        {
            Case.Nominative => "Nom",
            Case.Accusative => "Acc",
            Case.Instrumental => "Instr",
            Case.Dative => "Dat",
            Case.Ablative => "Abl",
            Case.Genitive => "Gen",
            Case.Locative => "Loc",
            Case.Vocative => "Voc",
            _ => @case.ToString()
        };

        var genderAbbr = gender switch
        {
            Gender.Masculine => "Masc",
            Gender.Feminine => "Fem",
            Gender.Neuter => "Neut",
            _ => gender.ToString()
        };

        var numberAbbr = number switch
        {
            Number.Singular => "Sg",
            Number.Plural => "Pl",
            _ => number.ToString()
        };

        return $"{caseAbbr} {genderAbbr} {numberAbbr}";
    }

    /// <summary>
    /// Gets abbreviated display name for a verb combo.
    /// Example: "Pr 3rd Sg" or "Pr 3rd Sg Reflx"
    /// </summary>
    public static string GetVerbComboShortName(Tense tense, Person person, Number number, Voice voice)
    {
        var tenseAbbr = tense switch
        {
            Tense.Present => "Pr",
            Tense.Imperative => "Imp",
            Tense.Optative => "Opt",
            Tense.Future => "Fut",
            _ => tense.ToString()
        };

        var personAbbr = person switch
        {
            Person.First => "1st",
            Person.Second => "2nd",
            Person.Third => "3rd",
            _ => person.ToString()
        };

        var numberAbbr = number switch
        {
            Number.Singular => "Sg",
            Number.Plural => "Pl",
            _ => number.ToString()
        };

        var baseName = $"{tenseAbbr} {personAbbr} {numberAbbr}";
        return voice == Voice.Reflexive ? $"{baseName} Reflx" : baseName;
    }
}
