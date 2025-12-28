namespace PaliPractice.Models.Inflection;

/// <summary>
/// Extension methods and helpers for NounPattern enum.
///
/// The enum uses breakpoint markers for efficient gender/regularity checks:
/// - pattern &lt; _RegularFem → Regular Masculine
/// - _RegularFem &lt; pattern &lt; _RegularNeut → Regular Feminine
/// - _RegularNeut &lt; pattern &lt; _Irregular → Regular Neuter
/// - pattern &gt;= _Irregular → Irregular (use ParentRegular() to get parent)
/// </summary>
public static class NounPatternHelper
{
    /// <summary>
    /// Parses database string to enum: "a masc" → NounPattern.AMasc
    /// </summary>
    public static NounPattern Parse(string dbString) => dbString switch
    {
        // Regular Masculine (traditional order: a, i, ī, u, ū, ar, ant, as)
        "a masc" => NounPattern.AMasc,
        "i masc" => NounPattern.IMasc,
        "ī masc" => NounPattern.ĪMasc,
        "u masc" => NounPattern.UMasc,
        "ū masc" => NounPattern.ŪMasc,
        "ar masc" => NounPattern.ArMasc,
        "ant masc" => NounPattern.AntMasc,
        "as masc" => NounPattern.AsMasc,

        // Regular Feminine (traditional order: ā, i, ī, u, ar)
        "ā fem" => NounPattern.ĀFem,
        "i fem" => NounPattern.IFem,
        "ī fem" => NounPattern.ĪFem,
        "u fem" => NounPattern.UFem,
        "ar fem" => NounPattern.ArFem,

        // Regular Neuter (traditional order: a, i, u)
        "a nt" => NounPattern.ANeut,
        "i nt" => NounPattern.INeut,
        "u nt" => NounPattern.UNeut,

        // Irregulars → AMasc
        "addha masc" => NounPattern.AddhaMasc,
        "a masc east" => NounPattern.AMascEast,
        "a masc pl" => NounPattern.AMascPl,
        "a2 masc" => NounPattern.A2Masc,
        "brahma masc" => NounPattern.BrahmaMasc,
        "go masc" => NounPattern.GoMasc,
        "rāja masc" => NounPattern.RājaMasc,
        "yuva masc" => NounPattern.YuvaMasc,

        // Irregulars → ĪMasc
        "ī masc pl" => NounPattern.ĪMascPl,

        // Irregulars → UMasc
        "jantu masc" => NounPattern.JantuMasc,
        "u masc pl" => NounPattern.UMascPl,

        // Irregulars → ArMasc
        "ar2 masc" => NounPattern.Ar2Masc,

        // Irregulars → AntMasc
        "anta masc" => NounPattern.AntaMasc,
        "arahant masc" => NounPattern.ArahantMasc,
        "bhavant masc" => NounPattern.BhavantMasc,
        "santa masc" => NounPattern.SantaMasc,

        // Irregulars → ĀFem
        "parisā fem" => NounPattern.ParisāFem,

        // Irregulars → IFem — jāti ends in short i
        "jāti fem" => NounPattern.JātiFem,
        "ratti fem" => NounPattern.RattiFem,

        // Irregulars → ĪFem
        "nadī fem" => NounPattern.NadīFem,
        "pokkharaṇī fem" => NounPattern.PokkharaṇīFem,

        // Irregulars → ArFem
        "mātar fem" => NounPattern.MātarFem,

        // Irregulars → ANeut
        "a nt east" => NounPattern.ANeutEast,
        "a nt irreg" => NounPattern.ANeutIrreg,
        "a nt pl" => NounPattern.ANeutPl,
        "kamma nt" => NounPattern.KammaNeut,

        _ => throw new ArgumentException($"Unknown noun pattern: {dbString}", nameof(dbString))
    };

    /// <summary>
    /// Tries to parse database string to enum. Returns false if unknown.
    /// </summary>
    public static bool TryParse(string dbString, out NounPattern pattern)
    {
        try
        {
            pattern = Parse(dbString);
            return true;
        }
        catch
        {
            pattern = default;
            return false;
        }
    }

    extension(NounPattern pattern)
    {
        /// <summary>
        /// Converts enum to database string: NounPattern.AMasc → "a masc"
        /// </summary>
        public string ToDbString() => pattern switch
        {
            // Regular Masculine (traditional order: a, i, ī, u, ū, ar, ant, as)
            NounPattern.AMasc => "a masc",
            NounPattern.IMasc => "i masc",
            NounPattern.ĪMasc => "ī masc",
            NounPattern.UMasc => "u masc",
            NounPattern.ŪMasc => "ū masc",
            NounPattern.ArMasc => "ar masc",
            NounPattern.AntMasc => "ant masc",
            NounPattern.AsMasc => "as masc",

            // Regular Feminine (traditional order: ā, i, ī, u, ar)
            NounPattern.ĀFem => "ā fem",
            NounPattern.IFem => "i fem",
            NounPattern.ĪFem => "ī fem",
            NounPattern.UFem => "u fem",
            NounPattern.ArFem => "ar fem",

            // Regular Neuter (traditional order: a, i, u)
            NounPattern.ANeut => "a nt",
            NounPattern.INeut => "i nt",
            NounPattern.UNeut => "u nt",

            // Irregulars → AMasc
            NounPattern.AddhaMasc => "addha masc",
            NounPattern.AMascEast => "a masc east",
            NounPattern.AMascPl => "a masc pl",
            NounPattern.A2Masc => "a2 masc",
            NounPattern.BrahmaMasc => "brahma masc",
            NounPattern.GoMasc => "go masc",
            NounPattern.RājaMasc => "rāja masc",
            NounPattern.YuvaMasc => "yuva masc",

            // Irregulars → ĪMasc
            NounPattern.ĪMascPl => "ī masc pl",

            // Irregulars → UMasc
            NounPattern.JantuMasc => "jantu masc",
            NounPattern.UMascPl => "u masc pl",

            // Irregulars → ArMasc
            NounPattern.Ar2Masc => "ar2 masc",

            // Irregulars → AntMasc
            NounPattern.AntaMasc => "anta masc",
            NounPattern.ArahantMasc => "arahant masc",
            NounPattern.BhavantMasc => "bhavant masc",
            NounPattern.SantaMasc => "santa masc",

            // Irregulars → ĀFem
            NounPattern.ParisāFem => "parisā fem",

            // Irregulars → IFem — jāti ends in short i
            NounPattern.JātiFem => "jāti fem",
            NounPattern.RattiFem => "ratti fem",

            // Irregulars → ĪFem
            NounPattern.NadīFem => "nadī fem",
            NounPattern.PokkharaṇīFem => "pokkharaṇī fem",

            // Irregulars → ArFem
            NounPattern.MātarFem => "mātar fem",

            // Irregulars → ANeut
            NounPattern.ANeutEast => "a nt east",
            NounPattern.ANeutIrreg => "a nt irreg",
            NounPattern.ANeutPl => "a nt pl",
            NounPattern.KammaNeut => "kamma nt",

            _ => throw new ArgumentOutOfRangeException(nameof(pattern), pattern, "Unknown or marker pattern")
        };

        /// <summary>
        /// Gets UI display label (stem only): NounPattern.AMasc → "a"
        /// </summary>
        public string ToDisplayLabel()
            => pattern.ToDbString().Split(' ')[0];

        /// <summary>
        /// Returns true if the pattern is irregular (pattern >= _Irregular).
        /// </summary>
        public bool IsIrregular()
            => pattern >= NounPattern._Irregular;

        /// <summary>
        /// Returns true if the pattern is plural-only (lacks singular forms).
        /// These are true "pluralia tantum" nouns.
        /// </summary>
        public bool IsPluralOnly() => pattern is
            NounPattern.AMascPl or
            NounPattern.ĪMascPl or
            NounPattern.UMascPl or
            NounPattern.ANeutPl;

        /// <summary>
        /// Gets the gender for a pattern using breakpoint comparisons.
        /// For irregular patterns, recursively gets parent's gender.
        /// </summary>
        public Gender GetGender()
        {
            // For irregulars, get the parent's gender
            if (pattern >= NounPattern._Irregular)
                return pattern.ParentRegular().GetGender();

            // Regular patterns - use breakpoint comparisons
            if (pattern < NounPattern._RegularFem)
                return Gender.Masculine;
            if (pattern < NounPattern._RegularNeut)
                return Gender.Feminine;
            if (pattern < NounPattern._Irregular)
                return Gender.Neuter;

            return Gender.None;
        }

        /// <summary>
        /// Gets the parent regular pattern for an irregular.
        /// Throws if called on a regular or breakpoint pattern.
        /// </summary>
        public NounPattern ParentRegular() => pattern switch
        {
            // Irregulars → AMasc
            NounPattern.RājaMasc or NounPattern.BrahmaMasc or NounPattern.A2Masc or
            NounPattern.AMascEast or NounPattern.AMascPl or NounPattern.AddhaMasc or
            NounPattern.GoMasc or NounPattern.YuvaMasc => NounPattern.AMasc,

            // Irregulars → ĪMasc
            NounPattern.ĪMascPl => NounPattern.ĪMasc,

            // Irregulars → UMasc
            NounPattern.JantuMasc or NounPattern.UMascPl => NounPattern.UMasc,

            // Irregulars → ArMasc
            NounPattern.Ar2Masc => NounPattern.ArMasc,

            // Irregulars → AntMasc
            NounPattern.AntaMasc or NounPattern.ArahantMasc or
            NounPattern.BhavantMasc or NounPattern.SantaMasc => NounPattern.AntMasc,

            // Irregulars → ĀFem
            NounPattern.ParisāFem => NounPattern.ĀFem,

            // Irregulars → IFem — jāti and ratti end in short i
            NounPattern.JātiFem or NounPattern.RattiFem => NounPattern.IFem,

            // Irregulars → ĪFem — nadī and pokkharaṇī end in long ī
            NounPattern.NadīFem or NounPattern.PokkharaṇīFem => NounPattern.ĪFem,

            // Irregulars → ArFem
            NounPattern.MātarFem => NounPattern.ArFem,

            // Irregulars → ANeut
            NounPattern.KammaNeut or NounPattern.ANeutEast or
            NounPattern.ANeutIrreg or NounPattern.ANeutPl => NounPattern.ANeut,

            // Regular or breakpoint patterns have no parent
            _ => throw new InvalidOperationException($"{pattern} is not an irregular pattern")
        };
    }
}
