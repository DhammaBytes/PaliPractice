namespace PaliPractice.Models.Inflection;

/// <summary>
/// Classification for noun declension patterns.
/// </summary>
public enum PatternType
{
    /// <summary>Common patterns with standard stem+ending construction.</summary>
    Base,
    /// <summary>Alternate ending tables, still computable from stem+ending.</summary>
    Variant,
    /// <summary>Forms must be read from database (DPD like='irreg').</summary>
    Irregular
}

/// <summary>
/// Extension methods and helpers for NounPattern enum.
///
/// Gender-based numbering (reflects Gender enum: Masc=1, Fem=2, Neut=3):
/// - 101-149 Base Masculine, 151-199 Variant Masculine
/// - 201-249 Base Feminine, 251-299 Variant Feminine (reserved)
/// - 301-349 Base Neuter, 351-399 Variant Neuter
/// - 1101+ Irregular Masculine, 1201+ Irregular Feminine, 1301+ Irregular Neuter
/// </summary>
public static class NounPatternHelper
{
    /// <summary>
    /// Parses database string to enum: "a masc" → NounPattern.AMasc
    /// </summary>
    public static NounPattern Parse(string dbString) => dbString switch
    {
        // Base Masculine
        "a masc" => NounPattern.AMasc,
        "i masc" => NounPattern.IMasc,
        "ī masc" => NounPattern.ĪMasc,
        "u masc" => NounPattern.UMasc,
        "ū masc" => NounPattern.ŪMasc,
        "ar masc" => NounPattern.ArMasc,
        "ant masc" => NounPattern.AntMasc,
        "as masc" => NounPattern.AsMasc,

        // Variant Masculine
        "a2 masc" => NounPattern.A2Masc,
        "a masc east" => NounPattern.AMascEast,
        "a masc pl" => NounPattern.AMascPl,
        "anta masc" => NounPattern.AntaMasc,
        "ar2 masc" => NounPattern.Ar2Masc,
        "ī masc pl" => NounPattern.ĪMascPl,
        "u masc pl" => NounPattern.UMascPl,

        // Base Feminine
        "ā fem" => NounPattern.ĀFem,
        "i fem" => NounPattern.IFem,
        "ī fem" => NounPattern.ĪFem,
        "u fem" => NounPattern.UFem,
        "ar fem" => NounPattern.ArFem,

        // Base Neuter
        "a nt" => NounPattern.ANeut,
        "i nt" => NounPattern.INeut,
        "u nt" => NounPattern.UNeut,

        // Variant Neuter
        "a nt east" => NounPattern.ANeutEast,
        "a nt irreg" => NounPattern.ANeutIrreg,
        "a nt pl" => NounPattern.ANeutPl,

        // Irregular Masculine (DPD like='irreg')
        "addha masc" => NounPattern.AddhaMasc,
        "arahant masc" => NounPattern.ArahantMasc,
        "bhavant masc" => NounPattern.BhavantMasc,
        "brahma masc" => NounPattern.BrahmaMasc,
        "go masc" => NounPattern.GoMasc,
        "jantu masc" => NounPattern.JantuMasc,
        "rāja masc" => NounPattern.RājaMasc,
        "santa masc" => NounPattern.SantaMasc,
        "yuva masc" => NounPattern.YuvaMasc,

        // Irregular Feminine (DPD like='irreg')
        "jāti fem" => NounPattern.JātiFem,
        "mātar fem" => NounPattern.MātarFem,
        "nadī fem" => NounPattern.NadīFem,
        "parisā fem" => NounPattern.ParisāFem,
        "pokkharaṇī fem" => NounPattern.PokkharaṇīFem,
        "ratti fem" => NounPattern.RattiFem,

        // Irregular Neuter (DPD like='irreg')
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
        /// Returns true if the pattern is None or a breakpoint marker (_VariantMasc, etc.).
        /// These values should never appear in runtime data.
        /// </summary>
        public bool IsMarkerOrNone()
            => pattern == NounPattern.None || pattern.ToString().StartsWith('_');

        /// <summary>
        /// Converts enum to database string: NounPattern.AMasc → "a masc"
        /// Throws for None or marker patterns.
        /// </summary>
        public string ToDbString() => pattern switch
        {
            // Base Masculine
            NounPattern.AMasc => "a masc",
            NounPattern.IMasc => "i masc",
            NounPattern.ĪMasc => "ī masc",
            NounPattern.UMasc => "u masc",
            NounPattern.ŪMasc => "ū masc",
            NounPattern.ArMasc => "ar masc",
            NounPattern.AntMasc => "ant masc",
            NounPattern.AsMasc => "as masc",

            // Variant Masculine
            NounPattern.A2Masc => "a2 masc",
            NounPattern.AMascEast => "a masc east",
            NounPattern.AMascPl => "a masc pl",
            NounPattern.AntaMasc => "anta masc",
            NounPattern.Ar2Masc => "ar2 masc",
            NounPattern.ĪMascPl => "ī masc pl",
            NounPattern.UMascPl => "u masc pl",

            // Base Feminine
            NounPattern.ĀFem => "ā fem",
            NounPattern.IFem => "i fem",
            NounPattern.ĪFem => "ī fem",
            NounPattern.UFem => "u fem",
            NounPattern.ArFem => "ar fem",

            // Base Neuter
            NounPattern.ANeut => "a nt",
            NounPattern.INeut => "i nt",
            NounPattern.UNeut => "u nt",

            // Variant Neuter
            NounPattern.ANeutEast => "a nt east",
            NounPattern.ANeutIrreg => "a nt irreg", // not irregular according to DPD inflection_templates
            NounPattern.ANeutPl => "a nt pl",

            // Irregular Masculine
            NounPattern.AddhaMasc => "addha masc",
            NounPattern.ArahantMasc => "arahant masc",
            NounPattern.BhavantMasc => "bhavant masc",
            NounPattern.BrahmaMasc => "brahma masc",
            NounPattern.GoMasc => "go masc",
            NounPattern.JantuMasc => "jantu masc",
            NounPattern.RājaMasc => "rāja masc",
            NounPattern.SantaMasc => "santa masc",
            NounPattern.YuvaMasc => "yuva masc",

            // Irregular Feminine
            NounPattern.JātiFem => "jāti fem",
            NounPattern.MātarFem => "mātar fem",
            NounPattern.NadīFem => "nadī fem",
            NounPattern.ParisāFem => "parisā fem",
            NounPattern.PokkharaṇīFem => "pokkharaṇī fem",
            NounPattern.RattiFem => "ratti fem",

            // Irregular Neuter
            NounPattern.KammaNeut => "kamma nt",

            _ => throw new ArgumentOutOfRangeException(nameof(pattern), pattern, "Unknown or marker pattern")
        };

        /// <summary>
        /// Gets UI display label (stem only): NounPattern.AMasc → "a"
        /// </summary>
        public string ToDisplayLabel()
            => pattern.ToDbString().Split(' ')[0];

        /// <summary>
        /// Returns true if the pattern is truly irregular (pattern > _Irregular).
        /// Irregular patterns have forms that must be read from database.
        /// </summary>
        public bool IsIrregular()
            => pattern > NounPattern._Irregular;

        /// <summary>
        /// Returns true if the pattern is a variant (alternate ending tables).
        /// Variant patterns use stem+ending construction but with non-standard endings.
        /// </summary>
        public bool IsVariant() => pattern switch
        {
            > NounPattern._VariantMasc and < NounPattern._BaseFem => true,
            > NounPattern._VariantFem and < NounPattern._BaseNeut => true,
            > NounPattern._VariantNeut and < NounPattern._Irregular => true,
            _ => false
        };

        /// <summary>
        /// Returns true if the pattern is a base pattern (standard stem+ending).
        /// Excludes None, breakpoint markers, variants, and irregulars.
        /// </summary>
        public bool IsBase()
            => !pattern.IsMarkerOrNone() && !pattern.IsIrregular() && !pattern.IsVariant();

        /// <summary>
        /// Gets the pattern type classification.
        /// Throws for None or marker patterns.
        /// </summary>
        public PatternType GetPatternType()
        {
            if (pattern.IsMarkerOrNone())
                throw new InvalidOperationException($"Cannot get PatternType for marker/None: {pattern}");

            if (pattern.IsIrregular()) return PatternType.Irregular;
            if (pattern.IsVariant()) return PatternType.Variant;
            return PatternType.Base;
        }

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
        /// Variants are within gender ranges, only irregulars need parent lookup.
        /// Throws for None or marker patterns.
        /// </summary>
        public Gender GetGender()
        {
            if (pattern.IsMarkerOrNone())
                throw new InvalidOperationException($"Cannot get Gender for marker/None: {pattern}");

            // Only irregulars need parent lookup (they're after _Irregular marker)
            if (pattern.IsIrregular())
                return pattern.ParentBase().GetGender();

            // Base + Variant patterns use breakpoint comparisons
            // Masculine: 101-199 (base + variant)
            if (pattern < NounPattern._BaseFem)
                return Gender.Masculine;
            // Feminine: 201-299 (base + variant reserved)
            if (pattern < NounPattern._BaseNeut)
                return Gender.Feminine;
            // Neuter: 301-399 (base + variant)
            return Gender.Neuter;
        }

        /// <summary>
        /// Gets the parent base pattern for a variant or irregular pattern.
        /// Throws if called on a base or breakpoint pattern.
        /// </summary>
        public NounPattern ParentBase() => pattern switch
        {
            // Variant Masculine → base patterns
            NounPattern.A2Masc or NounPattern.AMascEast or NounPattern.AMascPl => NounPattern.AMasc,
            NounPattern.AntaMasc => NounPattern.AntMasc,
            NounPattern.Ar2Masc => NounPattern.ArMasc,
            NounPattern.ĪMascPl => NounPattern.ĪMasc,
            NounPattern.UMascPl => NounPattern.UMasc,

            // Variant Neuter → base patterns
            NounPattern.ANeutEast or NounPattern.ANeutIrreg or NounPattern.ANeutPl => NounPattern.ANeut,

            // Irregular Masculine → base patterns
            NounPattern.AddhaMasc or NounPattern.BrahmaMasc or
            NounPattern.GoMasc or NounPattern.RājaMasc or NounPattern.YuvaMasc => NounPattern.AMasc,
            NounPattern.JantuMasc => NounPattern.UMasc,
            NounPattern.ArahantMasc or NounPattern.BhavantMasc or NounPattern.SantaMasc => NounPattern.AntMasc,

            // Irregular Feminine → base patterns
            NounPattern.JātiFem or NounPattern.RattiFem => NounPattern.IFem,
            NounPattern.MātarFem => NounPattern.ArFem,
            NounPattern.NadīFem or NounPattern.PokkharaṇīFem => NounPattern.ĪFem,
            NounPattern.ParisāFem => NounPattern.ĀFem,

            // Irregular Neuter → base patterns
            NounPattern.KammaNeut => NounPattern.ANeut,

            // Base or breakpoint patterns have no parent
            _ => throw new InvalidOperationException($"{pattern} is not a variant or irregular pattern")
        };
    }
}
