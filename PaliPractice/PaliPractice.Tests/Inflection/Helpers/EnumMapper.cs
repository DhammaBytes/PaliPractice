namespace PaliPractice.Tests.Inflection.Helpers;

/// <summary>
/// Maps DPD title strings (e.g., "masc nom sg", "pr 3rd sg") to PaliPractice enums.
/// </summary>
public static class EnumMapper
{
    /// <summary>
    /// Parse a noun title like "masc nom sg" into enum values.
    /// </summary>
    public static (Gender gender, Case nounCase, Number number) ParseNounTitle(string title)
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
    /// Note: Tense now includes traditional moods (imperative, optative).
    /// </summary>
    public static (Tense tense, Person person, Number number, bool reflexive) ParseVerbTitle(string title)
    {
        var parts = title.ToLower().Split(' ');

        bool reflexive;
        int offset;

        // Check for reflexive prefix
        if (parts[0] == "reflx")
        {
            reflexive = true;
            offset = 1; // Skip the "reflx" part
        }
        else
        {
            reflexive = false;
            offset = 0;
        }

        if (parts.Length < offset + 3)
        {
            throw new ArgumentException($"Invalid verb title format: {title}");
        }

        // Parse tense indicator (includes imperative and optative)
        var tenseIndicator = parts[offset];
        var tense = ParseTense(tenseIndicator);

        // Parse person (1st, 2nd, 3rd)
        var person = ParsePerson(parts[offset + 1]);

        // Parse number (sg, pl)
        var number = ParseNumber(parts[offset + 2]);

        return (tense, person, number, reflexive);
    }

    static Gender ParseGender(string gender)
    {
        return gender switch
        {
            "masc" => Gender.Masculine,
            "nt" => Gender.Neuter,
            "fem" => Gender.Feminine,
            _ => throw new ArgumentException($"Unknown gender: {gender}")
        };
    }

    static Case ParseCase(string caseStr)
    {
        return caseStr switch
        {
            "nom" => Case.Nominative,
            "acc" => Case.Accusative,
            "instr" => Case.Instrumental,
            "dat" => Case.Dative,
            "abl" => Case.Ablative,
            "gen" => Case.Genitive,
            "loc" => Case.Locative,
            "voc" => Case.Vocative,
            _ => throw new ArgumentException($"Unknown case: {caseStr}")
        };
    }

    static Number ParseNumber(string number)
    {
        return number switch
        {
            "sg" => Number.Singular,
            "pl" => Number.Plural,
            _ => throw new ArgumentException($"Unknown number: {number}")
        };
    }

    static Person ParsePerson(string person)
    {
        return person switch
        {
            "1st" => Person.First,
            "2nd" => Person.Second,
            "3rd" => Person.Third,
            _ => throw new ArgumentException($"Unknown person: {person}")
        };
    }

    static Tense ParseTense(string indicator)
    {
        return indicator switch
        {
            "pr" => Tense.Present,
            "imp" => Tense.Imperative,
            "opt" => Tense.Optative,
            "fut" => Tense.Future,
            "aor" => Tense.Aorist,
            _ => throw new ArgumentException($"Unknown tense indicator: {indicator}. Only pr, imp, opt, fut, aor are supported.")
        };
    }
}
