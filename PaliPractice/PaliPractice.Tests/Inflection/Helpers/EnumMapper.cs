namespace PaliPractice.Tests.Inflection.Helpers;

/// <summary>
/// Maps DPD title strings (e.g., "masc nom sg", "pr 3rd sg") to PaliPractice enums.
/// </summary>
public static class EnumMapper
{
    /// <summary>
    /// Parse a noun title like "masc nom sg" into enum values.
    /// </summary>
    public static (Gender gender, NounCase nounCase, Number number) ParseNounTitle(string title)
    {
        var parts = title.ToLower().Split(' ');

        if (parts.Length != 3)
        {
            throw new ArgumentException($"Invalid noun title format: {title}. Expected format: 'gender case number'");
        }

        var gender = ParseGender(parts[0]);
        var nounCase = ParseCase(parts[1]);
        var number = ParseNumber(parts[2]);

        return (gender, nounCase, number);
    }

    /// <summary>
    /// Parse a verb title like "pr 3rd sg" or "reflx opt 1st pl" into enum values.
    /// </summary>
    public static (Tense tense, Mood mood, Person person, Number number, Voice voice) ParseVerbTitle(string title)
    {
        var parts = title.ToLower().Split(' ');

        Voice voice;
        int offset;

        // Check for reflexive prefix
        if (parts[0] == "reflx")
        {
            voice = Voice.Reflexive;
            offset = 1; // Skip the "reflx" part
        }
        else
        {
            voice = Voice.Active;
            offset = 0;
        }

        if (parts.Length < offset + 3)
        {
            throw new ArgumentException($"Invalid verb title format: {title}");
        }

        // Parse tense/mood indicator (pr, fut, imp, opt)
        var tenseIndicator = parts[offset];
        var (tense, mood) = ParseTenseMood(tenseIndicator);

        // Parse person (1st, 2nd, 3rd)
        var person = ParsePerson(parts[offset + 1]);

        // Parse number (sg, pl)
        var number = ParseNumber(parts[offset + 2]);

        return (tense, mood, person, number, voice);
    }

    private static Gender ParseGender(string gender)
    {
        return gender switch
        {
            "masc" => Gender.Masculine,
            "nt" => Gender.Neuter,
            "fem" => Gender.Feminine,
            _ => throw new ArgumentException($"Unknown gender: {gender}")
        };
    }

    private static NounCase ParseCase(string caseStr)
    {
        return caseStr switch
        {
            "nom" => NounCase.Nominative,
            "acc" => NounCase.Accusative,
            "instr" => NounCase.Instrumental,
            "dat" => NounCase.Dative,
            "abl" => NounCase.Ablative,
            "gen" => NounCase.Genitive,
            "loc" => NounCase.Locative,
            "voc" => NounCase.Vocative,
            _ => throw new ArgumentException($"Unknown case: {caseStr}")
        };
    }

    private static Number ParseNumber(string number)
    {
        return number switch
        {
            "sg" => Number.Singular,
            "pl" => Number.Plural,
            _ => throw new ArgumentException($"Unknown number: {number}")
        };
    }

    private static Person ParsePerson(string person)
    {
        return person switch
        {
            "1st" => Person.First,
            "2nd" => Person.Second,
            "3rd" => Person.Third,
            _ => throw new ArgumentException($"Unknown person: {person}")
        };
    }

    private static (Tense tense, Mood mood) ParseTenseMood(string indicator)
    {
        return indicator switch
        {
            "pr" => (Tense.Present, Mood.Indicative),
            "fut" => (Tense.Future, Mood.Indicative),
            "imp" => (Tense.Present, Mood.Imperative),
            "opt" => (Tense.Present, Mood.Optative),
            "aor" => (Tense.Aorist, Mood.Indicative),
            "perf" => (Tense.Perfect, Mood.Indicative),
            "imperf" => (Tense.Imperfect, Mood.Indicative),
            _ => throw new ArgumentException($"Unknown tense/mood indicator: {indicator}")
        };
    }
}
