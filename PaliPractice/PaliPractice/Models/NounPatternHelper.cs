namespace PaliPractice.Models;

/// <summary>
/// Extension methods and helpers for NounPattern enum.
///
/// Pattern Hierarchy:
/// - Regular patterns (AMasc, IMasc, etc.) appear as UI checkboxes grouped by gender
/// - Irregular patterns are children of regular patterns and inherit their checkbox state
/// - Use ParentRegular() to get an irregular's parent, ChildIrregulars() to get a regular's children
/// - Special patterns like DviCard have no parent and should be handled separately (always excluded)
/// - When filtering by checkbox selection, map irregulars to their parent's checkbox
/// </summary>
public static class NounPatternHelper
{
    /// <summary>
    /// Masculine regular patterns that appear as UI checkboxes.
    /// </summary>
    public static readonly NounPattern[] MasculinePatterns =
        [NounPattern.AMasc, NounPattern.IMasc, NounPattern.ĪMasc,
         NounPattern.UMasc, NounPattern.ŪMasc, NounPattern.AsMasc,
         NounPattern.ArMasc, NounPattern.AntMasc];

    /// <summary>
    /// Neuter regular patterns that appear as UI checkboxes.
    /// </summary>
    public static readonly NounPattern[] NeuterPatterns =
        [NounPattern.ANeut, NounPattern.INeut, NounPattern.UNeut];

    /// <summary>
    /// Feminine regular patterns that appear as UI checkboxes.
    /// </summary>
    public static readonly NounPattern[] FemininePatterns =
        [NounPattern.ĀFem, NounPattern.IFem, NounPattern.ĪFem,
         NounPattern.UFem, NounPattern.ArFem];

    /// <summary>
    /// All regular patterns.
    /// </summary>
    public static readonly NounPattern[] RegularPatterns =
        [.. MasculinePatterns, .. NeuterPatterns, .. FemininePatterns];

    /// <summary>
    /// Irregular patterns that map to regular patterns.
    /// </summary>
    public static readonly NounPattern[] IrregularPatterns =
        [// → AMasc
         NounPattern.RājaMasc, NounPattern.BrahmaMasc, NounPattern.A2Masc,
         NounPattern.AMascEast, NounPattern.AMascPl, NounPattern.AddhaMasc,
         NounPattern.GoMasc, NounPattern.YuvaMasc,
         // → ĪMasc
         NounPattern.ĪMascPl,
         // → UMasc
         NounPattern.JantuMasc, NounPattern.UMascPl,
         // → ArMasc
         NounPattern.Ar2Masc,
         // → AntMasc
         NounPattern.AntaMasc, NounPattern.ArahantMasc, NounPattern.BhavantMasc, NounPattern.SantaMasc,
         // → ANeut
         NounPattern.KammaNeut, NounPattern.ANeutEast, NounPattern.ANeutIrreg, NounPattern.ANeutPl,
         // → ĀFem
         NounPattern.ParisāFem,
         // → IFem
         NounPattern.RattiFem,
         // → ĪFem
         NounPattern.JātiFem, NounPattern.NadīFem, NounPattern.PokkharaṇīFem,
         // → ArFem
         NounPattern.MātarFem,
         // Special
         NounPattern.DviCard];

    /// <summary>
    /// All patterns (regular + irregular).
    /// </summary>
    public static readonly NounPattern[] AllPatterns =
        [.. RegularPatterns, .. IrregularPatterns];

    /// <summary>
    /// Converts enum to database string: NounPattern.AMasc → "a masc"
    /// </summary>
    public static string ToDbString(this NounPattern pattern) => pattern switch
    {
        // Regular Masculine
        NounPattern.AMasc => "a masc",
        NounPattern.IMasc => "i masc",
        NounPattern.ĪMasc => "ī masc",
        NounPattern.UMasc => "u masc",
        NounPattern.ŪMasc => "ū masc",
        NounPattern.AsMasc => "as masc",
        NounPattern.ArMasc => "ar masc",
        NounPattern.AntMasc => "ant masc",

        // Regular Neuter
        NounPattern.ANeut => "a nt",
        NounPattern.INeut => "i nt",
        NounPattern.UNeut => "u nt",

        // Regular Feminine
        NounPattern.ĀFem => "ā fem",
        NounPattern.IFem => "i fem",
        NounPattern.ĪFem => "ī fem",
        NounPattern.UFem => "u fem",
        NounPattern.ArFem => "ar fem",

        // Irregulars → AMasc
        NounPattern.RājaMasc => "rāja masc",
        NounPattern.BrahmaMasc => "brahma masc",
        NounPattern.A2Masc => "a2 masc",
        NounPattern.AMascEast => "a masc east",
        NounPattern.AMascPl => "a masc pl",
        NounPattern.AddhaMasc => "addha masc",
        NounPattern.GoMasc => "go masc",
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

        // Irregulars → ANeut
        NounPattern.KammaNeut => "kamma nt",
        NounPattern.ANeutEast => "a nt east",
        NounPattern.ANeutIrreg => "a nt irreg",
        NounPattern.ANeutPl => "a nt pl",

        // Irregulars → ĀFem
        NounPattern.ParisāFem => "parisā fem",

        // Irregulars → IFem
        NounPattern.RattiFem => "ratti fem",

        // Irregulars → ĪFem
        NounPattern.JātiFem => "jāti fem",
        NounPattern.NadīFem => "nadī fem",
        NounPattern.PokkharaṇīFem => "pokkharaṇī fem",

        // Irregulars → ArFem
        NounPattern.MātarFem => "mātar fem",

        // Special
        NounPattern.DviCard => "dvi card",

        _ => throw new ArgumentOutOfRangeException(nameof(pattern), pattern, "Unknown noun pattern")
    };

    /// <summary>
    /// Parses database string to enum: "a masc" → NounPattern.AMasc
    /// </summary>
    public static NounPattern Parse(string dbString) => dbString switch
    {
        // Regular Masculine
        "a masc" => NounPattern.AMasc,
        "i masc" => NounPattern.IMasc,
        "ī masc" => NounPattern.ĪMasc,
        "u masc" => NounPattern.UMasc,
        "ū masc" => NounPattern.ŪMasc,
        "as masc" => NounPattern.AsMasc,
        "ar masc" => NounPattern.ArMasc,
        "ant masc" => NounPattern.AntMasc,

        // Regular Neuter
        "a nt" => NounPattern.ANeut,
        "i nt" => NounPattern.INeut,
        "u nt" => NounPattern.UNeut,

        // Regular Feminine
        "ā fem" => NounPattern.ĀFem,
        "i fem" => NounPattern.IFem,
        "ī fem" => NounPattern.ĪFem,
        "u fem" => NounPattern.UFem,
        "ar fem" => NounPattern.ArFem,

        // Irregulars → AMasc
        "rāja masc" => NounPattern.RājaMasc,
        "brahma masc" => NounPattern.BrahmaMasc,
        "a2 masc" => NounPattern.A2Masc,
        "a masc east" => NounPattern.AMascEast,
        "a masc pl" => NounPattern.AMascPl,
        "addha masc" => NounPattern.AddhaMasc,
        "go masc" => NounPattern.GoMasc,
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

        // Irregulars → ANeut
        "kamma nt" => NounPattern.KammaNeut,
        "a nt east" => NounPattern.ANeutEast,
        "a nt irreg" => NounPattern.ANeutIrreg,
        "a nt pl" => NounPattern.ANeutPl,

        // Irregulars → ĀFem
        "parisā fem" => NounPattern.ParisāFem,

        // Irregulars → IFem
        "ratti fem" => NounPattern.RattiFem,

        // Irregulars → ĪFem
        "jāti fem" => NounPattern.JātiFem,
        "nadī fem" => NounPattern.NadīFem,
        "pokkharaṇī fem" => NounPattern.PokkharaṇīFem,

        // Irregulars → ArFem
        "mātar fem" => NounPattern.MātarFem,

        // Special
        "dvi card" => NounPattern.DviCard,

        _ => throw new ArgumentException($"Unknown noun pattern: {dbString}", nameof(dbString))
    };

    /// <summary>
    /// Tries to parse database string to enum. Returns false if unknown.
    /// </summary>
    public static bool TryParse(string dbString, out NounPattern pattern)
    {
        pattern = default;
        try
        {
            pattern = Parse(dbString);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets patterns for a specific gender (regular only, for UI).
    /// </summary>
    public static NounPattern[] GetPatternsForGender(Gender gender) => gender switch
    {
        Gender.Masculine => MasculinePatterns,
        Gender.Neuter => NeuterPatterns,
        Gender.Feminine => FemininePatterns,
        _ => []
    };

    extension(NounPattern pattern)
    {
        /// <summary>
        /// Gets UI display label (stem only): NounPattern.AMasc → "a"
        /// </summary>
        public string ToDisplayLabel()
            => pattern.ToDbString().Split(' ')[0];

        /// <summary>
        /// Gets the gender for a pattern.
        /// </summary>
        public Gender GetGender() => pattern switch
        {
            NounPattern.AMasc or NounPattern.IMasc or NounPattern.ĪMasc or
            NounPattern.UMasc or NounPattern.ŪMasc or NounPattern.AsMasc or
            NounPattern.ArMasc or NounPattern.AntMasc or
            NounPattern.RājaMasc or NounPattern.BrahmaMasc or NounPattern.A2Masc or
            NounPattern.AMascEast or NounPattern.AMascPl or NounPattern.AddhaMasc or
            NounPattern.GoMasc or NounPattern.YuvaMasc or NounPattern.ĪMascPl or
            NounPattern.JantuMasc or NounPattern.UMascPl or NounPattern.Ar2Masc or
            NounPattern.AntaMasc or NounPattern.ArahantMasc or NounPattern.BhavantMasc or
            NounPattern.SantaMasc => Gender.Masculine,

            NounPattern.ANeut or NounPattern.INeut or NounPattern.UNeut or
            NounPattern.KammaNeut or NounPattern.ANeutEast or NounPattern.ANeutIrreg or
            NounPattern.ANeutPl => Gender.Neuter,

            NounPattern.ĀFem or NounPattern.IFem or NounPattern.ĪFem or
            NounPattern.UFem or NounPattern.ArFem or
            NounPattern.ParisāFem or NounPattern.RattiFem or
            NounPattern.JātiFem or NounPattern.NadīFem or NounPattern.PokkharaṇīFem or
            NounPattern.MātarFem => Gender.Feminine,

            NounPattern.DviCard => Gender.None,

            _ => Gender.None
        };

        /// <summary>
        /// Returns true if the pattern is irregular.
        /// Note: Uses "not regular" check rather than explicit irregular list
        /// so new irregular patterns are automatically detected. This means
        /// new REGULAR patterns must be added to this list explicitly.
        /// </summary>
        public bool IsIrregular() => pattern switch
        {
            // Regular patterns - add new regular patterns here
            NounPattern.AMasc or NounPattern.IMasc or NounPattern.ĪMasc or
            NounPattern.UMasc or NounPattern.ŪMasc or NounPattern.AsMasc or
            NounPattern.ArMasc or NounPattern.AntMasc or
            NounPattern.ANeut or NounPattern.INeut or NounPattern.UNeut or
            NounPattern.ĀFem or NounPattern.IFem or NounPattern.ĪFem or
            NounPattern.UFem or NounPattern.ArFem => false,
            // Everything else is irregular (including special patterns like DviCard)
            _ => true
        };

        /// <summary>
        /// Gets the parent regular pattern for an irregular.
        /// Throws if called on a regular pattern.
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

            // Irregulars → ANeut
            NounPattern.KammaNeut or NounPattern.ANeutEast or
            NounPattern.ANeutIrreg or NounPattern.ANeutPl => NounPattern.ANeut,

            // Irregulars → ĀFem
            NounPattern.ParisāFem => NounPattern.ĀFem,

            // Irregulars → IFem
            NounPattern.RattiFem => NounPattern.IFem,

            // Irregulars → ĪFem
            NounPattern.JātiFem or NounPattern.NadīFem or
            NounPattern.PokkharaṇīFem => NounPattern.ĪFem,

            // Irregulars → ArFem
            NounPattern.MātarFem => NounPattern.ArFem,

            // DviCard has no parent - special case
            NounPattern.DviCard => throw new InvalidOperationException(
                $"{pattern} is a special pattern with no regular parent"),

            // Regular patterns have no parent
            _ => throw new InvalidOperationException($"{pattern} is a regular pattern, not an irregular")
        };

        /// <summary>
        /// Gets all child irregular patterns for a regular pattern.
        /// Throws if called on an irregular pattern.
        /// </summary>
        public HashSet<NounPattern> ChildIrregulars() => pattern switch
        {
            // Masculine
            NounPattern.AMasc => [NounPattern.RājaMasc, NounPattern.BrahmaMasc, NounPattern.A2Masc,
                                  NounPattern.AMascEast, NounPattern.AMascPl, NounPattern.AddhaMasc,
                                  NounPattern.GoMasc, NounPattern.YuvaMasc],
            NounPattern.IMasc => [],
            NounPattern.ĪMasc => [NounPattern.ĪMascPl],
            NounPattern.UMasc => [NounPattern.JantuMasc, NounPattern.UMascPl],
            NounPattern.ŪMasc => [],
            NounPattern.AsMasc => [],
            NounPattern.ArMasc => [NounPattern.Ar2Masc],
            NounPattern.AntMasc => [NounPattern.AntaMasc, NounPattern.ArahantMasc,
                                    NounPattern.BhavantMasc, NounPattern.SantaMasc],

            // Neuter
            NounPattern.ANeut => [NounPattern.KammaNeut, NounPattern.ANeutEast,
                                  NounPattern.ANeutIrreg, NounPattern.ANeutPl],
            NounPattern.INeut => [],
            NounPattern.UNeut => [],

            // Feminine
            NounPattern.ĀFem => [NounPattern.ParisāFem],
            NounPattern.IFem => [NounPattern.RattiFem],
            NounPattern.ĪFem => [NounPattern.JātiFem, NounPattern.NadīFem, NounPattern.PokkharaṇīFem],
            NounPattern.UFem => [],
            NounPattern.ArFem => [NounPattern.MātarFem],

            _ => throw new InvalidOperationException($"{pattern} is an irregular pattern, not a regular")
        };
    }
}
