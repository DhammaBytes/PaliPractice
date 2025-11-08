namespace PaliPractice.Models.Inflection;

/// <summary>
/// Provides verb conjugation endings for common patterns.
/// Covers 3 regular present tense patterns: ati pr, eti pr, oti pr.
/// Aorist and irregular patterns are excluded due to high variability.
/// </summary>
public static class VerbPatterns
{
    /// <summary>
    /// Get conjugation endings for a verb pattern.
    /// </summary>
    /// <param name="pattern">Pattern name (e.g., "ati pr", "eti pr")</param>
    /// <param name="person">First, second, or third person</param>
    /// <param name="number">Singular or plural</param>
    /// <param name="tense">Tense (includes traditional moods: present, imperative, optative, future, aorist)</param>
    /// <param name="voice">Voice (active, reflexive, passive, causative)</param>
    /// <returns>Array of endings. Empty array if combination not supported.</returns>
    public static string[] GetEndings(
        string pattern,
        Person person,
        Number number,
        Tense tense,
        Voice voice)
    {
        return pattern switch
        {
            "ati pr" => GetAti_Pr(person, number, tense, voice),
            "eti pr" => GetEti_Pr(person, number, tense, voice),
            "oti pr" => GetOti_Pr(person, number, tense, voice),
            _ => Array.Empty<string>()
        };
    }

    private static string[] GetAti_Pr(Person person, Number number, Tense tense, Voice voice)
    {
        // Present tense, active voice
        if (tense == Tense.Present && voice == Voice.Active)
        {
            return (person, number) switch
            {
                (Person.Third, Number.Singular) => ["ati"],
                (Person.Third, Number.Plural) => ["anti"],
                (Person.Second, Number.Singular) => ["asi"],
                (Person.Second, Number.Plural) => ["atha"],
                (Person.First, Number.Singular) => ["āmi"],
                (Person.First, Number.Plural) => ["āma"],
                _ => Array.Empty<string>()
            };
        }

        // Present tense, reflexive voice
        if (tense == Tense.Present && voice == Voice.Reflexive)
        {
            return (person, number) switch
            {
                (Person.Third, Number.Singular) => ["ate"],
                (Person.Third, Number.Plural) => ["ante", "are"],
                (Person.Second, Number.Singular) => ["ase"],
                (Person.Second, Number.Plural) => ["avhe"],
                (Person.First, Number.Singular) => ["e"],
                (Person.First, Number.Plural) => ["amhase", "āmhe"],
                _ => Array.Empty<string>()
            };
        }

        // Imperative tense, active voice
        if (tense == Tense.Imperative && voice == Voice.Active)
        {
            return (person, number) switch
            {
                (Person.Third, Number.Singular) => ["atu"],
                (Person.Third, Number.Plural) => ["antu"],
                (Person.Second, Number.Singular) => ["a", "āsi", "āhi"],
                (Person.Second, Number.Plural) => ["atha"],
                (Person.First, Number.Singular) => ["āmi"],
                (Person.First, Number.Plural) => ["āma"],
                _ => Array.Empty<string>()
            };
        }

        // Imperative tense, reflexive voice
        if (tense == Tense.Imperative && voice == Voice.Reflexive)
        {
            return (person, number) switch
            {
                (Person.Third, Number.Singular) => ["ataṃ"],
                (Person.Third, Number.Plural) => ["antaṃ", "aruṃ"],
                (Person.Second, Number.Singular) => ["assu"],
                (Person.Second, Number.Plural) => ["avho"],
                (Person.First, Number.Singular) => ["e"],
                (Person.First, Number.Plural) => ["āmase"],
                _ => Array.Empty<string>()
            };
        }

        // Optative tense, active voice
        if (tense == Tense.Optative && voice == Voice.Active)
        {
            return (person, number) switch
            {
                (Person.Third, Number.Singular) => ["e", "eyya"],
                (Person.Third, Number.Plural) => ["eyyu", "eyyuṃ"],
                (Person.Second, Number.Singular) => ["e", "eyyāsi"],
                (Person.Second, Number.Plural) => ["etha", "eyyātha"],
                (Person.First, Number.Singular) => ["e", "eyyāmi"],
                (Person.First, Number.Plural) => ["ema", "emu", "eyyāma"],
                _ => Array.Empty<string>()
            };
        }

        // Optative tense, reflexive voice
        if (tense == Tense.Optative && voice == Voice.Reflexive)
        {
            return (person, number) switch
            {
                (Person.Third, Number.Singular) => ["etha"],
                (Person.Third, Number.Plural) => ["eraṃ"],
                (Person.Second, Number.Singular) => ["etho", "eyyātho"],
                (Person.Second, Number.Plural) => ["eyyavho", "eyyāvho"],
                (Person.First, Number.Singular) => ["eyyaṃ"],
                (Person.First, Number.Plural) => ["emase", "eyyāmhe"],
                _ => Array.Empty<string>()
            };
        }

        // Future tense, active voice
        if (tense == Tense.Future && voice == Voice.Active)
        {
            return (person, number) switch
            {
                (Person.Third, Number.Singular) => ["issati"],
                (Person.Third, Number.Plural) => ["issanti"],
                (Person.Second, Number.Singular) => ["issasi"],
                (Person.Second, Number.Plural) => ["issatha"],
                (Person.First, Number.Singular) => ["issāmi"],
                (Person.First, Number.Plural) => ["issāma"],
                _ => Array.Empty<string>()
            };
        }

        // Future tense, reflexive voice
        if (tense == Tense.Future && voice == Voice.Reflexive)
        {
            return (person, number) switch
            {
                (Person.Third, Number.Singular) => ["issate"],
                (Person.Third, Number.Plural) => ["issante", "issare"],
                (Person.Second, Number.Singular) => ["issase"],
                (Person.Second, Number.Plural) => ["issavhe"],
                (Person.First, Number.Singular) => ["issaṃ"],
                (Person.First, Number.Plural) => ["issāmase", "issāmhe"],
                _ => Array.Empty<string>()
            };
        }

        return Array.Empty<string>();
    }

    private static string[] GetEti_Pr(Person person, Number number, Tense tense, Voice voice)
    {
        // Present tense, active voice (no reflexive forms for present indicative)
        if (tense == Tense.Present && voice == Voice.Active)
        {
            return (person, number) switch
            {
                (Person.Third, Number.Singular) => ["eti"],
                (Person.Third, Number.Plural) => ["enti"],
                (Person.Second, Number.Singular) => ["esi"],
                (Person.Second, Number.Plural) => ["etha"],
                (Person.First, Number.Singular) => ["emi"],
                (Person.First, Number.Plural) => ["ema"],
                _ => Array.Empty<string>()
            };
        }

        // Imperative tense, active voice
        if (tense == Tense.Imperative && voice == Voice.Active)
        {
            return (person, number) switch
            {
                (Person.Third, Number.Singular) => ["etu"],
                (Person.Third, Number.Plural) => ["entu"],
                (Person.Second, Number.Singular) => ["ehi"],
                (Person.Second, Number.Plural) => ["etha"],
                (Person.First, Number.Singular) => ["emi"],
                (Person.First, Number.Plural) => ["ema"],
                _ => Array.Empty<string>()
            };
        }

        // Imperative tense, reflexive voice
        if (tense == Tense.Imperative && voice == Voice.Reflexive)
        {
            return (person, number) switch
            {
                (Person.Third, Number.Singular) => ["etaṃ"],
                (Person.Third, Number.Plural) => ["entaṃ"],
                (Person.Second, Number.Singular) => ["assu", "essu"],
                (Person.Second, Number.Plural) => ["avho", "evho"],
                (Person.First, Number.Singular) => ["e"],
                (Person.First, Number.Plural) => ["amase", "emase"],
                _ => Array.Empty<string>()
            };
        }

        // Optative tense, active voice
        if (tense == Tense.Optative && voice == Voice.Active)
        {
            return (person, number) switch
            {
                (Person.Third, Number.Singular) => ["e", "eyya"],
                (Person.Third, Number.Plural) => ["eyyuṃ"],
                (Person.Second, Number.Singular) => ["e", "eyyāsi"],
                (Person.Second, Number.Plural) => ["etha", "eyyātha"],
                (Person.First, Number.Singular) => ["e", "eyyāmi"],
                (Person.First, Number.Plural) => ["ema", "emu", "eyyāma"],
                _ => Array.Empty<string>()
            };
        }

        // Optative tense, reflexive voice
        if (tense == Tense.Optative && voice == Voice.Reflexive)
        {
            return (person, number) switch
            {
                (Person.Third, Number.Singular) => ["etha"],
                (Person.Third, Number.Plural) => ["eraṃ"],
                (Person.Second, Number.Singular) => ["etho", "eyyātho"],
                (Person.Second, Number.Plural) => ["eyyavho", "eyyāvho"],
                (Person.First, Number.Singular) => ["eyyaṃ"],
                (Person.First, Number.Plural) => ["eyyāmhe"],
                _ => Array.Empty<string>()
            };
        }

        // Future tense, active voice
        if (tense == Tense.Future && voice == Voice.Active)
        {
            return (person, number) switch
            {
                (Person.Third, Number.Singular) => ["issati", "essati"],
                (Person.Third, Number.Plural) => ["issanti", "essanti"],
                (Person.Second, Number.Singular) => ["issasi", "essasi"],
                (Person.Second, Number.Plural) => ["issatha", "essatha"],
                (Person.First, Number.Singular) => ["issāmi", "essāmi"],
                (Person.First, Number.Plural) => ["issāma", "essāma"],
                _ => Array.Empty<string>()
            };
        }

        // Future tense, reflexive voice
        if (tense == Tense.Future && voice == Voice.Reflexive)
        {
            return (person, number) switch
            {
                (Person.Third, Number.Singular) => ["essate"],
                (Person.Third, Number.Plural) => ["essante", "essare"],
                (Person.Second, Number.Singular) => ["essase"],
                (Person.Second, Number.Plural) => ["essavhe"],
                (Person.First, Number.Singular) => ["essaṃ"],
                (Person.First, Number.Plural) => ["essāmhe"],
                _ => Array.Empty<string>()
            };
        }

        return Array.Empty<string>();
    }

    private static string[] GetOti_Pr(Person person, Number number, Tense tense, Voice voice)
    {
        // Present tense, active voice
        if (tense == Tense.Present && voice == Voice.Active)
        {
            return (person, number) switch
            {
                (Person.Third, Number.Singular) => ["oti"],
                (Person.Third, Number.Plural) => ["onti"],
                (Person.Second, Number.Singular) => ["osi"],
                (Person.Second, Number.Plural) => ["otha"],
                (Person.First, Number.Singular) => ["omi"],
                (Person.First, Number.Plural) => ["oma"],
                _ => Array.Empty<string>()
            };
        }

        // Imperative tense, active voice
        if (tense == Tense.Imperative && voice == Voice.Active)
        {
            return (person, number) switch
            {
                (Person.Third, Number.Singular) => ["otu"],
                (Person.Third, Number.Plural) => ["ontu"],
                (Person.Second, Number.Singular) => ["a", "ohi"],
                (Person.Second, Number.Plural) => ["otha"],
                (Person.First, Number.Singular) => ["omi"],
                (Person.First, Number.Plural) => ["oma"],
                _ => Array.Empty<string>()
            };
        }

        // Optative tense, active voice
        if (tense == Tense.Optative && voice == Voice.Active)
        {
            return (person, number) switch
            {
                (Person.Third, Number.Singular) => ["e", "eyya"],
                (Person.Third, Number.Plural) => ["eyyuṃ"],
                (Person.Second, Number.Singular) => ["e", "eyyāsi"],
                (Person.Second, Number.Plural) => ["etha", "eyyātha"],
                (Person.First, Number.Singular) => ["e", "eyyaṃ", "eyyāmi"],
                (Person.First, Number.Plural) => ["ema", "emu", "eyyāma"],
                _ => Array.Empty<string>()
            };
        }

        // Optative tense, reflexive voice
        if (tense == Tense.Optative && voice == Voice.Reflexive)
        {
            return (person, number) switch
            {
                (Person.Third, Number.Singular) => ["etha"],
                (Person.Third, Number.Plural) => ["eraṃ"],
                (Person.Second, Number.Singular) => ["etho", "eyyātho"],
                (Person.Second, Number.Plural) => ["eyyavho", "eyyāvho"],
                (Person.First, Number.Singular) => ["eyyaṃ"],
                (Person.First, Number.Plural) => ["eyyāmhe"],
                _ => Array.Empty<string>()
            };
        }

        // Future tense, active voice
        if (tense == Tense.Future && voice == Voice.Active)
        {
            return (person, number) switch
            {
                (Person.Third, Number.Singular) => ["issati"],
                (Person.Third, Number.Plural) => ["issanti"],
                (Person.Second, Number.Singular) => ["issasi"],
                (Person.Second, Number.Plural) => ["issatha"],
                (Person.First, Number.Singular) => ["issāmi"],
                (Person.First, Number.Plural) => ["issāma"],
                _ => Array.Empty<string>()
            };
        }

        // Future tense, reflexive voice
        if (tense == Tense.Future && voice == Voice.Reflexive)
        {
            return (person, number) switch
            {
                (Person.Third, Number.Singular) => ["issate"],
                (Person.Third, Number.Plural) => ["issante", "issare"],
                (Person.Second, Number.Singular) => ["issase"],
                (Person.Second, Number.Plural) => ["issavhe"],
                (Person.First, Number.Singular) => ["issaṃ"],
                (Person.First, Number.Plural) => ["issāmhe"],
                _ => Array.Empty<string>()
            };
        }

        return Array.Empty<string>();
    }
}
