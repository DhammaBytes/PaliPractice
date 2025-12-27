namespace PaliPractice.Models.Inflection;

/// <summary>
/// Irregular verb conjugation patterns.
/// These are word-specific patterns that don't follow regular rules.
/// </summary>
public static partial class VerbEndings
{
    #region Irregular patterns

    /// <summary>hoti pr - irregular (√bhū, to be)</summary>
    static string[] GetHoti_Pr(Person person, Number number, Tense tense, bool reflexive)
    {
        // No reflexive forms for hoti
        if (reflexive) return [];

        // Present tense
        if (tense == Tense.Present)
        {
            return (person, number) switch
            {
                (Person.Third, Number.Singular) => ["hoti"],
                (Person.Third, Number.Plural) => ["honti"],
                (Person.Second, Number.Singular) => ["hosi"],
                (Person.Second, Number.Plural) => ["hotha"],
                (Person.First, Number.Singular) => ["homi"],
                (Person.First, Number.Plural) => ["homa"],
                _ => []
            };
        }

        // Imperative
        if (tense == Tense.Imperative)
        {
            return (person, number) switch
            {
                (Person.Third, Number.Singular) => ["hotu"],
                (Person.Third, Number.Plural) => ["hontu"],
                (Person.Second, Number.Singular) => ["hohi"],
                (Person.Second, Number.Plural) => ["hotha"],
                (Person.First, Number.Singular) => ["homi"],
                (Person.First, Number.Plural) => ["homa"],
                _ => []
            };
        }

        return [];
    }

    /// <summary>atthi pr - irregular (√as, to be/exist)</summary>
    static string[] GetAtthi_Pr(Person person, Number number, Tense tense, bool reflexive)
    {
        // No reflexive forms for atthi
        if (reflexive) return [];

        // Present tense
        if (tense == Tense.Present)
        {
            return (person, number) switch
            {
                (Person.Third, Number.Singular) => ["atthi"],
                (Person.Third, Number.Plural) => ["atthi", "santi", "sante"],
                (Person.Second, Number.Singular) => ["asi"],
                (Person.Second, Number.Plural) => ["attha"],
                (Person.First, Number.Singular) => ["amhi", "asmi"],
                (Person.First, Number.Plural) => ["amha", "amhase", "amhā", "amhāsi", "asma", "asmase", "asmā"],
                _ => []
            };
        }

        // Imperative
        if (tense == Tense.Imperative)
        {
            return (person, number) switch
            {
                (Person.Third, Number.Singular) => ["atthu"],
                (Person.Third, Number.Plural) => ["santu"],
                (Person.Second, Number.Singular) => ["āhi"],
                (Person.Second, Number.Plural) => ["attha"],
                (Person.First, Number.Singular) => ["amhi", "asmi"],
                (Person.First, Number.Plural) => ["amha", "amhā", "asma", "asmā"],
                _ => []
            };
        }

        return [];
    }

    /// <summary>karoti pr - irregular (√kar, to do/make)</summary>
    static string[] GetKaroti_Pr(Person person, Number number, Tense tense, bool reflexive)
    {
        // Present tense, active
        if (tense == Tense.Present && !reflexive)
        {
            return (person, number) switch
            {
                (Person.Third, Number.Singular) => ["karoti"],
                (Person.Third, Number.Plural) => ["karonti"],
                (Person.Second, Number.Singular) => ["karosi"],
                (Person.Second, Number.Plural) => ["karotha"],
                (Person.First, Number.Singular) => ["karomi"],
                (Person.First, Number.Plural) => ["karoma"],
                _ => []
            };
        }

        // Present tense, reflexive
        if (tense == Tense.Present && reflexive)
        {
            return (person, number) switch
            {
                (Person.Third, Number.Singular) => ["kurute"],
                (Person.Third, Number.Plural) => ["kurunte"],
                (Person.Second, Number.Singular) => ["kuruse"],
                (Person.Second, Number.Plural) => ["kuruvhe"],
                (Person.First, Number.Singular) => ["kare"],
                (Person.First, Number.Plural) => ["kurumhe"],
                _ => []
            };
        }

        // Imperative, active
        if (tense == Tense.Imperative && !reflexive)
        {
            return (person, number) switch
            {
                (Person.Third, Number.Singular) => ["karotu"],
                (Person.Third, Number.Plural) => ["karontu"],
                (Person.Second, Number.Singular) => ["karohi"],
                (Person.Second, Number.Plural) => ["karotha"],
                (Person.First, Number.Singular) => ["karomi"],
                (Person.First, Number.Plural) => ["karoma"],
                _ => []
            };
        }

        // Imperative, reflexive
        if (tense == Tense.Imperative && reflexive)
        {
            return (person, number) switch
            {
                (Person.Third, Number.Singular) => ["kurutaṃ"],
                (Person.Third, Number.Plural) => ["kuruntaṃ"],
                (Person.Second, Number.Singular) => ["karassu", "kurussu"],
                (Person.Second, Number.Plural) => ["kuruvho"],
                (Person.First, Number.Singular) => ["kare"],
                (Person.First, Number.Plural) => ["karomasi", "karomase"],
                _ => []
            };
        }

        // Optative, active
        if (tense == Tense.Optative && !reflexive)
        {
            return (person, number) switch
            {
                (Person.Third, Number.Singular) => ["kare", "kareyya"],
                (Person.Third, Number.Plural) => ["kareyyuṃ"],
                (Person.Second, Number.Singular) => ["kareyyāsi"],
                (Person.Second, Number.Plural) => ["kareyyātha"],
                (Person.First, Number.Singular) => ["kareyyāmi"],
                (Person.First, Number.Plural) => ["kareyyāma"],
                _ => []
            };
        }

        // Optative, reflexive
        if (tense == Tense.Optative && reflexive)
        {
            return (person, number) switch
            {
                (Person.Third, Number.Singular) => ["kayirātha"],
                (Person.First, Number.Singular) => ["kare", "kareyyaṃ"],
                (Person.First, Number.Plural) => ["kareyyāmhe"],
                _ => []
            };
        }

        // Future, active
        if (tense == Tense.Future && !reflexive)
        {
            return (person, number) switch
            {
                (Person.Third, Number.Singular) => ["karissati"],
                (Person.Third, Number.Plural) => ["karissanti"],
                (Person.Second, Number.Singular) => ["karissasi"],
                (Person.Second, Number.Plural) => ["karissatha"],
                (Person.First, Number.Singular) => ["karissāmi"],
                (Person.First, Number.Plural) => ["karissāma"],
                _ => []
            };
        }

        // Future, reflexive
        if (tense == Tense.Future && reflexive)
        {
            return (person, number) switch
            {
                (Person.Third, Number.Singular) => ["karissate"],
                (Person.Third, Number.Plural) => ["karissante"],
                (Person.Second, Number.Singular) => ["karissase"],
                (Person.Second, Number.Plural) => ["karissavhe"],
                (Person.First, Number.Singular) => ["karissaṃ"],
                (Person.First, Number.Plural) => ["karissāmhe"],
                _ => []
            };
        }

        return [];
    }

    /// <summary>brūti pr - irregular (√brū, to say/speak)</summary>
    static string[] GetBruti_Pr(Person person, Number number, Tense tense, bool reflexive)
    {
        // Present tense, active
        if (tense == Tense.Present && !reflexive)
        {
            return (person, number) switch
            {
                (Person.Third, Number.Singular) => ["bravīti", "brūti"],
                (Person.Third, Number.Plural) => ["brunti", "bruvanti"],
                (Person.Second, Number.Singular) => ["brūsi"],
                (Person.Second, Number.Plural) => ["brūtha"],
                (Person.First, Number.Singular) => ["brūmi"],
                (Person.First, Number.Plural) => ["brūma"],
                _ => []
            };
        }

        // Present tense, reflexive
        if (tense == Tense.Present && reflexive)
        {
            return (person, number) switch
            {
                (Person.Third, Number.Singular) => ["brūte"],
                (Person.Third, Number.Plural) => ["bruvante"],
                (Person.Second, Number.Singular) => ["brūse"],
                (Person.Second, Number.Plural) => ["bruvhe"],
                (Person.First, Number.Singular) => ["bruve"],
                (Person.First, Number.Plural) => ["brumhe"],
                _ => []
            };
        }

        // Imperative, active
        if (tense == Tense.Imperative && !reflexive)
        {
            return (person, number) switch
            {
                (Person.Third, Number.Singular) => ["bravitu", "brūtu"],
                (Person.Third, Number.Plural) => ["bruvantu"],
                (Person.Second, Number.Singular) => ["brūhi"],
                (Person.Second, Number.Plural) => ["brūtha"],
                (Person.First, Number.Singular) => ["brūmi"],
                (Person.First, Number.Plural) => ["brūma"],
                _ => []
            };
        }

        // Imperative, reflexive
        if (tense == Tense.Imperative && reflexive)
        {
            return (person, number) switch
            {
                (Person.Third, Number.Singular) => ["brūte"],
                (Person.Third, Number.Plural) => ["bruvante"],
                (Person.Second, Number.Singular) => ["brūse"],
                (Person.Second, Number.Plural) => ["brūvhe"],
                (Person.First, Number.Singular) => ["brūve"],
                (Person.First, Number.Plural) => ["brumhe"],
                _ => []
            };
        }

        // Optative, active
        if (tense == Tense.Optative && !reflexive)
        {
            return (person, number) switch
            {
                (Person.Third, Number.Singular) => ["bruve", "bruveyya"],
                (Person.Third, Number.Plural) => ["bruveyyuṃ"],
                (Person.Second, Number.Singular) => ["bruveyyāsi"],
                (Person.Second, Number.Plural) => ["bruveyyātha"],
                (Person.First, Number.Singular) => ["bruveyyāmi"],
                (Person.First, Number.Plural) => ["bruveyyāma"],
                _ => []
            };
        }

        // Optative, reflexive
        if (tense == Tense.Optative && reflexive)
        {
            return (person, number) switch
            {
                (Person.Third, Number.Singular) => ["bruvetha"],
                (Person.Third, Number.Plural) => ["bruveraṃ"],
                (Person.Second, Number.Singular) => ["bruvetho"],
                (Person.Second, Number.Plural) => ["bruveyyavho"],
                (Person.First, Number.Singular) => ["bruveyyaṃ"],
                (Person.First, Number.Plural) => ["bruveyyāmhe"],
                _ => []
            };
        }

        // Future, active
        if (tense == Tense.Future && !reflexive)
        {
            return (person, number) switch
            {
                (Person.Third, Number.Singular) => ["bravissati"],
                (Person.Third, Number.Plural) => ["bravissanti"],
                (Person.Second, Number.Singular) => ["bravissasi"],
                (Person.Second, Number.Plural) => ["bravissatha"],
                (Person.First, Number.Singular) => ["bravissāmi"],
                (Person.First, Number.Plural) => ["bravissāma"],
                _ => []
            };
        }

        // Future, reflexive
        if (tense == Tense.Future && reflexive)
        {
            return (person, number) switch
            {
                (Person.Third, Number.Singular) => ["bravissate"],
                (Person.Third, Number.Plural) => ["bravissante"],
                (Person.Second, Number.Singular) => ["bravissase"],
                (Person.Second, Number.Plural) => ["bravissavhe"],
                (Person.First, Number.Singular) => ["bravissaṃ"],
                (Person.First, Number.Plural) => ["bravissāmhe"],
                _ => []
            };
        }

        return [];
    }

    #endregion
}
